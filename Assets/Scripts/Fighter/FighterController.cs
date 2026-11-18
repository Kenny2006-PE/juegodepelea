using System.Collections.Generic;
using UnityEngine;

namespace FightingGame
{
    /// <summary>
    /// Decoupled 2.5D fighting character controller.
    /// Handles X-axis movement, automatic opponent facing, combat state machine,
    /// Animation Event & fallback hitbox timing, blocking, hitstun, knockdown invulnerability, and KO.
    /// Can be driven interchangeably by FighterInputHandler (P1/P2) or FighterAIController (CPU).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HealthSystem))]
    public class FighterController : MonoBehaviour
    {
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashIsBlocking = Animator.StringToHash("isBlocking");
        private static readonly int HashIsDead = Animator.StringToHash("isDead");
        private static readonly int HashJab = Animator.StringToHash("Jab");
        private static readonly int HashKick = Animator.StringToHash("Kick");
        private static readonly int HashLightHit = Animator.StringToHash("LightHit");
        private static readonly int HashKnockdown = Animator.StringToHash("Knockdown");
        private static readonly int HashKO = Animator.StringToHash("KO");

        private static readonly int StateLocomotion = Animator.StringToHash("Locomotion");
        private static readonly int StateStunned = Animator.StringToHash("Stunned");
        private static readonly int StateGettingUp = Animator.StringToHash("Getting Up");

        [Header("Identity")]
        [SerializeField] private FighterId fighterId = FighterId.Tanjiro;

        [Header("Component References")]
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private HealthSystem healthSystem;
        [SerializeField] private Hurtbox hurtbox;
        [SerializeField] private List<Hitbox> hitboxes = new List<Hitbox>();

        [Header("Movement & Arena Bounds")]
        [SerializeField] private float forwardMoveSpeed = 3.2f;
        [SerializeField] private float backwardMoveSpeed = 2.4f;
        [SerializeField] private float turnSpeed = 16f;
        [SerializeField] private float stageHalfWidth = 6.8f;
        [SerializeField] private float minFighterSeparation = 0.95f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float knockbackDrag = 14f;

        [Header("Combat Attacks")]
        [SerializeField] private AttackData jabAttack = AttackData.CreateJab();
        [SerializeField] private AttackData kickAttack = AttackData.CreateKick();

        [Header("Recovery & Invulnerability")]
        [SerializeField] private float knockdownMinDuration = 0.55f;
        [SerializeField] private float knockdownMaxDuration = 4.6f;
        [SerializeField] private float wakeupInvulnerabilityGrace = 0.2f;

        public FighterId FighterId => fighterId;
        public FighterState CurrentState { get; private set; } = FighterState.Idle;
        public HealthSystem Health => healthSystem;
        public FighterController Opponent { get; private set; }
        public IFighterInput InputSource { get; private set; }
        public int FacingDirection { get; private set; } = 1; // +1 = Right, -1 = Left
        public bool IsControlLocked { get; private set; } = true;
        public bool HasLandedCurrentAttack => hitTargetsThisAttack.Count > 0;
        public AttackType CurrentAttackType => currentAttack.attackType;

        private CombatFeedback combatFeedback;
        private FightingCamera fightingCamera;

        private readonly HashSet<FighterController> hitTargetsThisAttack = new HashSet<FighterController>();

        private float verticalVelocity;
        private float knockbackVelocityX;
        private float animSpeedCurrent;

        private AttackData currentAttack;
        private float attackElapsed;
        private bool receivedAnimEventThisAttack;
        private bool fallbackHitboxOpened;
        private bool fallbackHitboxClosed;

        private float hitstunTimer;
        private float knockdownElapsed;
        private bool seenKnockdownAnimState;
        private float wakeupGraceTimer;

        private void Awake()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (hurtbox == null) hurtbox = GetComponentInChildren<Hurtbox>();

            if (hitboxes == null || hitboxes.Count == 0)
            {
                hitboxes = new List<Hitbox>(GetComponentsInChildren<Hitbox>(true));
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
                if (animator.GetComponent<FighterAnimationEvents>() == null)
                {
                    animator.gameObject.AddComponent<FighterAnimationEvents>();
                }
            }

            if (hurtbox != null)
            {
                hurtbox.Configure(this);
            }

            DeactivateAllHitboxes();
        }

        public void InitializeMatchReferences(
            FighterController opponent,
            IFighterInput inputSource,
            CombatFeedback feedback,
            FightingCamera cam)
        {
            Opponent = opponent;
            InputSource = inputSource;
            combatFeedback = feedback;
            fightingCamera = cam;
        }

        public void SetFighterId(FighterId id)
        {
            fighterId = id;
        }

        public void SetReferences(Animator anim, Hurtbox bodyHurtbox, List<Hitbox> limbHitboxes)
        {
            animator = anim;
            hurtbox = bodyHurtbox;
            hitboxes = limbHitboxes ?? new List<Hitbox>();
        }

        public void SetControlLocked(bool locked)
        {
            IsControlLocked = locked;
            InputSource?.SetInputEnabled(!locked);

            if (locked)
            {
                InputSource?.ClearBufferedInputs();
                if (CurrentState == FighterState.Moving || CurrentState == FighterState.Blocking)
                {
                    SetState(FighterState.Idle);
                }

                if (animator != null)
                {
                    animator.SetFloat(HashSpeed, 0f);
                    animator.SetBool(HashIsBlocking, false);
                }
            }
        }

        public void ResetForNewRound(Vector3 spawnPosition, bool faceRight)
        {
            DeactivateAllHitboxes();
            hitTargetsThisAttack.Clear();

            knockbackVelocityX = 0f;
            verticalVelocity = 0f;
            animSpeedCurrent = 0f;
            attackElapsed = 0f;
            hitstunTimer = 0f;
            knockdownElapsed = 0f;
            wakeupGraceTimer = 0f;
            seenKnockdownAnimState = false;

            healthSystem.ResetHealth();
            FacingDirection = faceRight ? 1 : -1;

            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
                transform.rotation = Quaternion.Euler(0f, faceRight ? 90f : -90f, 0f);
                characterController.enabled = true;
            }
            else
            {
                transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
                transform.rotation = Quaternion.Euler(0f, faceRight ? 90f : -90f, 0f);
            }

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.SetBool(HashIsDead, false);
                animator.SetBool(HashIsBlocking, false);
                animator.SetFloat(HashSpeed, 0f);
            }

            InputSource?.ClearBufferedInputs();
            SetState(FighterState.Idle);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            UpdateWakeupGrace(dt);
            UpdateFacing(dt);

            switch (CurrentState)
            {
                case FighterState.Idle:
                case FighterState.Moving:
                    UpdateNeutralState(dt);
                    break;

                case FighterState.Blocking:
                    UpdateBlockingState(dt);
                    break;

                case FighterState.Attacking:
                    UpdateAttackingState(dt);
                    break;

                case FighterState.Hitstun:
                    UpdateHitstunState(dt);
                    break;

                case FighterState.KnockedDown:
                    UpdateKnockedDownState(dt);
                    break;

                case FighterState.Dead:
                    UpdateDeadState(dt);
                    break;
            }

            ApplyHorizontalKnockbackAndGravity(dt);
            EnforcePlaneAndSeparation();
        }

        private void UpdateWakeupGrace(float dt)
        {
            if (wakeupGraceTimer > 0f)
            {
                wakeupGraceTimer -= dt;
                if (wakeupGraceTimer <= 0f && CurrentState != FighterState.KnockedDown)
                {
                    healthSystem.IsInvulnerable = false;
                }
            }
        }

        private void UpdateFacing(float dt)
        {
            if (Opponent == null || CurrentState == FighterState.Dead || CurrentState == FighterState.KnockedDown)
            {
                return;
            }

            // Keep facing locked mid-attack so swings don't flip awkwardly mid-frame
            if (CurrentState == FighterState.Idle || CurrentState == FighterState.Moving || CurrentState == FighterState.Blocking)
            {
                float dx = Opponent.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.05f)
                {
                    FacingDirection = dx >= 0f ? 1 : -1;
                }
            }

            Quaternion targetRot = Quaternion.Euler(0f, FacingDirection >= 0 ? 90f : -90f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * turnSpeed);
        }

        private void UpdateNeutralState(float dt)
        {
            if (IsControlLocked || InputSource == null)
            {
                UpdateAnimatorLocomotion(0f, false, dt);
                return;
            }

            // Priority 1: Attacks
            if (TryStartAttackFromInput(allowAll: true))
            {
                return;
            }

            // Priority 2: Blocking
            if (InputSource.IsBlocking)
            {
                SetState(FighterState.Blocking);
                UpdateAnimatorLocomotion(0f, true, dt);
                return;
            }

            // Priority 3: Movement on X axis
            float moveX = Mathf.Clamp(InputSource.MoveX, -1f, 1f);
            if (Mathf.Abs(moveX) > 0.05f)
            {
                SetState(FighterState.Moving);

                float relativeDir = moveX * FacingDirection; // +1 forward toward opponent, -1 backward
                float moveSpeed = relativeDir >= 0f ? forwardMoveSpeed : backwardMoveSpeed;

                MoveCharacter(new Vector3(moveX * moveSpeed * dt, 0f, 0f));
                UpdateAnimatorLocomotion(relativeDir, false, dt);
            }
            else
            {
                SetState(FighterState.Idle);
                UpdateAnimatorLocomotion(0f, false, dt);
            }
        }

        private void UpdateBlockingState(float dt)
        {
            UpdateAnimatorLocomotion(0f, true, dt);

            if (IsControlLocked || InputSource == null || !InputSource.IsBlocking)
            {
                SetState(FighterState.Idle);
                if (animator != null)
                {
                    animator.SetBool(HashIsBlocking, false);
                }
            }
        }

        private void UpdateAttackingState(float dt)
        {
            attackElapsed += dt;
            UpdateAnimatorLocomotion(0f, false, dt);

            // Allow Jab -> Roundhouse Kick combo cancel cleanly after Jab connects or in late Jab window
            if (!IsControlLocked && InputSource != null
                && currentAttack.attackType == AttackType.Jab
                && (HasLandedCurrentAttack || attackElapsed >= currentAttack.fallbackDuration * 0.42f)
                && InputSource.ConsumeKick())
            {
                StartAttack(kickAttack);
                return;
            }

            float normalizedProgress = GetCurrentAttackNormalizedTime();

            // Synchronized fallback hitbox window (protects against clips without Animation Events)
            if (!receivedAnimEventThisAttack)
            {
                if (!fallbackHitboxOpened && normalizedProgress >= currentAttack.activeStartNormalized)
                {
                    fallbackHitboxOpened = true;
                    ActivateAttackHitboxes(currentAttack);
                }

                if (fallbackHitboxOpened && !fallbackHitboxClosed && normalizedProgress >= currentAttack.activeEndNormalized)
                {
                    fallbackHitboxClosed = true;
                    DeactivateAllHitboxes();
                }
            }

            // End attack once animation completes or fallback cap is reached
            bool animFinished = HasAttackAnimationFinished();
            if (animFinished || attackElapsed >= currentAttack.fallbackDuration * 1.45f)
            {
                EndCurrentAttack();
            }
        }

        private void UpdateHitstunState(float dt)
        {
            UpdateAnimatorLocomotion(0f, false, dt);
            hitstunTimer -= dt;
            if (hitstunTimer <= 0f)
            {
                SetState(FighterState.Idle);
            }
        }

        private void UpdateKnockedDownState(float dt)
        {
            UpdateAnimatorLocomotion(0f, false, dt);
            healthSystem.IsInvulnerable = true;
            knockdownElapsed += dt;

            if (animator != null)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                int nameHash = info.shortNameHash;
                bool inKnockdownChain = nameHash == StateStunned || nameHash == StateGettingUp;

                if (inKnockdownChain)
                {
                    seenKnockdownAnimState = true;
                }

                bool returnedToLocomotion = seenKnockdownAnimState
                    && !inKnockdownChain
                    && !animator.IsInTransition(0)
                    && nameHash == StateLocomotion;

                if ((returnedToLocomotion && knockdownElapsed >= knockdownMinDuration)
                    || knockdownElapsed >= knockdownMaxDuration)
                {
                    FinishKnockdown();
                }
            }
            else if (knockdownElapsed >= 2.2f)
            {
                FinishKnockdown();
            }
        }

        private void FinishKnockdown()
        {
            SetState(FighterState.Idle);
            wakeupGraceTimer = wakeupInvulnerabilityGrace;
            healthSystem.IsInvulnerable = wakeupInvulnerabilityGrace > 0f;
            InputSource?.ClearBufferedInputs();
        }

        private void UpdateDeadState(float dt)
        {
            UpdateAnimatorLocomotion(0f, false, dt);
            DeactivateAllHitboxes();
        }

        private bool TryStartAttackFromInput(bool allowAll)
        {
            if (InputSource == null) return false;

            if (allowAll && InputSource.ConsumeKick())
            {
                StartAttack(kickAttack);
                return true;
            }

            if (InputSource.ConsumeJab())
            {
                StartAttack(jabAttack);
                return true;
            }

            return false;
        }

        private void StartAttack(AttackData attack)
        {
            DeactivateAllHitboxes();
            hitTargetsThisAttack.Clear();

            currentAttack = attack;
            attackElapsed = 0f;
            receivedAnimEventThisAttack = false;
            fallbackHitboxOpened = false;
            fallbackHitboxClosed = false;

            SetState(FighterState.Attacking);

            if (animator != null)
            {
                animator.SetBool(HashIsBlocking, false);
                animator.SetFloat(HashSpeed, 0f);
                animator.ResetTrigger(HashJab);
                animator.ResetTrigger(HashKick);

                switch (attack.attackType)
                {
                    case AttackType.Jab:
                        animator.SetTrigger(HashJab);
                        break;
                    case AttackType.Kick:
                        animator.SetTrigger(HashKick);
                        break;
                }
            }
        }

        private void EndCurrentAttack()
        {
            DeactivateAllHitboxes();
            if (CurrentState == FighterState.Attacking)
            {
                SetState(FighterState.Idle);
            }
        }

        // === Animation Event Callbacks (called via FighterAnimationEvents) ===
        public void OnAnimEventEnableHitbox()
        {
            if (CurrentState != FighterState.Attacking) return;
            receivedAnimEventThisAttack = true;
            ActivateAttackHitboxes(currentAttack);
        }

        public void OnAnimEventDisableHitbox()
        {
            receivedAnimEventThisAttack = true;
            DeactivateAllHitboxes();
        }

        public void OnAnimEventAttackEnd()
        {
            if (CurrentState == FighterState.Attacking)
            {
                EndCurrentAttack();
            }
        }

        public bool TryClaimHitTarget(FighterController target)
        {
            if (target == null || target == this) return false;
            return hitTargetsThisAttack.Add(target);
        }

        public bool IsBlockingFrontally(FighterController attacker)
        {
            if (CurrentState != FighterState.Blocking || attacker == null)
            {
                return false;
            }

            float dirToAttacker = attacker.transform.position.x - transform.position.x;
            return dirToAttacker * FacingDirection >= -0.15f;
        }

        public void OnReceiveHit(AttackData attack, FighterController attacker, Vector3 hitPoint)
        {
            if (CurrentState == FighterState.Dead || healthSystem.IsDead || healthSystem.IsInvulnerable)
            {
                return;
            }

            bool blocked = IsBlockingFrontally(attacker);
            float appliedDamage = healthSystem.TakeDamage(attack.damage, blocked);
            if (appliedDamage <= 0f && !blocked)
            {
                return;
            }

            float pushDir = attacker != null
                ? Mathf.Sign(transform.position.x - attacker.transform.position.x)
                : -FacingDirection;
            if (Mathf.Approximately(pushDir, 0f)) pushDir = -FacingDirection;

            combatFeedback?.PlayHitFeedback(hitPoint, attack.attackType, blocked);

            if (healthSystem.IsDead)
            {
                EnterDeadState(pushDir, attack.knockbackForce);
                return;
            }

            if (blocked)
            {
                // Frontal block: 85% damage reduced, NO hitstun or knockdown, light guard pushback
                knockbackVelocityX = pushDir * (attack.knockbackForce * 0.32f);
                fightingCamera?.TriggerShake(0.05f, 0.08f);
                return;
            }

            // Unblocked hit interrupts attacks cleanly
            DeactivateAllHitboxes();

            if (attack.causesKnockdown)
            {
                EnterKnockdownState(pushDir, attack.knockbackForce);
                fightingCamera?.TriggerShake(0.18f, 0.22f);
            }
            else
            {
                EnterHitstunState(pushDir, attack.knockbackForce, attack.hitstunDuration);
                float shake = attack.attackType == AttackType.Kick ? 0.11f : 0.06f;
                fightingCamera?.TriggerShake(shake, 0.12f);
            }
        }

        private void EnterHitstunState(float pushDir, float knockback, float duration)
        {
            SetState(FighterState.Hitstun);
            hitstunTimer = Mathf.Max(0.15f, duration);
            knockbackVelocityX = pushDir * knockback;

            if (animator != null)
            {
                animator.SetBool(HashIsBlocking, false);
                animator.ResetTrigger(HashJab);
                animator.ResetTrigger(HashKick);
                animator.SetTrigger(HashLightHit);
            }
        }

        private void EnterKnockdownState(float pushDir, float knockback)
        {
            SetState(FighterState.KnockedDown);
            healthSystem.IsInvulnerable = true;
            knockdownElapsed = 0f;
            seenKnockdownAnimState = false;
            knockbackVelocityX = pushDir * knockback;

            if (animator != null)
            {
                animator.SetBool(HashIsBlocking, false);
                animator.ResetTrigger(HashJab);
                animator.ResetTrigger(HashKick);
                animator.ResetTrigger(HashLightHit);
                animator.SetTrigger(HashKnockdown);
            }
        }

        private void EnterDeadState(float pushDir, float knockback)
        {
            SetState(FighterState.Dead);
            DeactivateAllHitboxes();
            healthSystem.IsInvulnerable = true;
            knockbackVelocityX = pushDir * Mathf.Max(knockback, 4.5f);

            combatFeedback?.PlayKOFeedback(transform.position);
            fightingCamera?.TriggerShake(0.28f, 0.35f);

            if (animator != null)
            {
                animator.SetBool(HashIsBlocking, false);
                animator.SetBool(HashIsDead, true);
                animator.ResetTrigger(HashLightHit);
                animator.ResetTrigger(HashKnockdown);
                animator.SetTrigger(HashKO);
            }
        }

        private void ActivateAttackHitboxes(AttackData attack)
        {
            for (int i = 0; i < hitboxes.Count; i++)
            {
                Hitbox hb = hitboxes[i];
                if (hb == null) continue;

                if ((attack.activeLimbs & hb.LimbType) != 0)
                {
                    hb.Activate(attack);
                }
                else
                {
                    hb.Deactivate();
                }
            }
        }

        private void DeactivateAllHitboxes()
        {
            for (int i = 0; i < hitboxes.Count; i++)
            {
                if (hitboxes[i] != null)
                {
                    hitboxes[i].Deactivate();
                }
            }
        }

        private float GetCurrentAttackNormalizedTime()
        {
            if (animator != null)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(currentAttack.animatorStateName))
                {
                    return Mathf.Clamp01(info.normalizedTime);
                }
            }

            return currentAttack.fallbackDuration > 0f
                ? Mathf.Clamp01(attackElapsed / currentAttack.fallbackDuration)
                : 1f;
        }

        private bool HasAttackAnimationFinished()
        {
            if (animator == null)
            {
                return attackElapsed >= currentAttack.fallbackDuration;
            }

            if (attackElapsed < 0.14f)
            {
                return false;
            }

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(currentAttack.animatorStateName))
            {
                return info.normalizedTime >= 0.94f && !animator.IsInTransition(0);
            }

            if (info.shortNameHash == StateLocomotion && !animator.IsInTransition(0) && attackElapsed >= 0.25f)
            {
                return true;
            }

            return false;
        }

        private void UpdateAnimatorLocomotion(float targetSpeed, bool blocking, float dt)
        {
            if (animator == null) return;

            animSpeedCurrent = Mathf.MoveTowards(animSpeedCurrent, targetSpeed, dt * 10f);
            animator.SetFloat(HashSpeed, animSpeedCurrent);
            animator.SetBool(HashIsBlocking, blocking);
        }

        private void ApplyHorizontalKnockbackAndGravity(float dt)
        {
            if (characterController == null || !characterController.enabled) return;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * dt;
            }

            Vector3 motion = new Vector3(knockbackVelocityX * dt, verticalVelocity * dt, 0f);
            characterController.Move(motion);

            knockbackVelocityX = Mathf.MoveTowards(knockbackVelocityX, 0f, knockbackDrag * dt);
        }

        private void MoveCharacter(Vector3 delta)
        {
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(delta);
            }
            else
            {
                transform.position += delta;
            }
        }

        private void EnforcePlaneAndSeparation()
        {
            Vector3 pos = transform.position;
            pos.z = 0f;
            pos.x = Mathf.Clamp(pos.x, -stageHalfWidth, stageHalfWidth);

            // Prevent standing fighters from passing directly through one another
            if (Opponent != null
                && CurrentState != FighterState.Dead
                && Opponent.CurrentState != FighterState.Dead
                && CurrentState != FighterState.KnockedDown
                && Opponent.CurrentState != FighterState.KnockedDown)
            {
                float dx = pos.x - Opponent.transform.position.x;
                float absDx = Mathf.Abs(dx);
                if (absDx < minFighterSeparation)
                {
                    float pushDir = absDx > 0.001f ? Mathf.Sign(dx) : -FacingDirection;
                    float overlap = (minFighterSeparation - absDx) * 0.5f;
                    pos.x = Mathf.Clamp(pos.x + pushDir * overlap, -stageHalfWidth, stageHalfWidth);
                }
            }

            if (Mathf.Abs(pos.x - transform.position.x) > 0.0001f || Mathf.Abs(pos.z - transform.position.z) > 0.0001f)
            {
                if (characterController != null && characterController.enabled)
                {
                    characterController.enabled = false;
                    transform.position = pos;
                    characterController.enabled = true;
                }
                else
                {
                    transform.position = pos;
                }
            }
        }

        private void SetState(FighterState newState)
        {
            CurrentState = newState;
        }
    }
}
