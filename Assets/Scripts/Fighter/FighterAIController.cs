using UnityEngine;

namespace FightingGame
{
    /// <summary>
    /// Tactical fighting game AI implementing IFighterInput.
    /// Fights exclusively using Jab Cross, Jab -> Roundhouse Kick combos,
    /// standalone Roundhouse Kick at mid range, spacing steps, and reactive Center Block
    /// tuned by AIDifficulty (Easy / Normal / Hard).
    /// </summary>
    public class FighterAIController : MonoBehaviour, IFighterInput
    {
        [Header("AI Settings")]
        [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;

        [Header("Ranges")]
        [SerializeField] private float punchRange = 1.55f;
        [SerializeField] private float kickRange = 2.20f;
        [SerializeField] private float threatBlockRange = 2.40f;
        [SerializeField] private float idealSpacing = 1.60f;

        private FighterController selfFighter;
        private bool inputEnabled;

        private float decisionCooldown;
        private float blockHoldTimer;
        private float retreatTimer;
        private bool pendingKickCombo;
        private bool evaluatedCurrentEnemyAttack;

        private bool queuedJab;
        private bool queuedKick;

        public float MoveX { get; private set; }
        public bool IsBlocking { get; private set; }

        // Difficulty-scaled parameters
        private float minDecisionInterval;
        private float maxDecisionInterval;
        private float blockChance;
        private float comboFollowUpChance;
        private float aggression;

        public void Configure(FighterController owner, AIDifficulty aiDifficulty)
        {
            selfFighter = owner;
            difficulty = aiDifficulty;
            ApplyDifficultyTuning();
            ClearBufferedInputs();
        }

        private void Awake()
        {
            if (selfFighter == null)
            {
                selfFighter = GetComponent<FighterController>();
            }
            ApplyDifficultyTuning();
        }

        private void ApplyDifficultyTuning()
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    minDecisionInterval = 0.55f;
                    maxDecisionInterval = 0.95f;
                    blockChance = 0.25f;
                    comboFollowUpChance = 0.25f;
                    aggression = 0.48f;
                    break;

                case AIDifficulty.Normal:
                    minDecisionInterval = 0.28f;
                    maxDecisionInterval = 0.56f;
                    blockChance = 0.55f;
                    comboFollowUpChance = 0.62f;
                    aggression = 0.70f;
                    break;

                case AIDifficulty.Hard:
                    minDecisionInterval = 0.14f;
                    maxDecisionInterval = 0.32f;
                    blockChance = 0.82f;
                    comboFollowUpChance = 0.86f;
                    aggression = 0.88f;
                    break;
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                MoveX = 0f;
                IsBlocking = false;
                ClearBufferedInputs();
            }
        }

        public void ClearBufferedInputs()
        {
            queuedJab = false;
            queuedKick = false;
            pendingKickCombo = false;
            blockHoldTimer = 0f;
            retreatTimer = 0f;
            evaluatedCurrentEnemyAttack = false;
        }

        public bool ConsumeJab()
        {
            if (queuedJab)
            {
                queuedJab = false;
                return true;
            }
            return false;
        }

        public bool ConsumeKick()
        {
            if (queuedKick)
            {
                queuedKick = false;
                return true;
            }
            return false;
        }

        private void Update()
        {
            if (!inputEnabled || selfFighter == null || selfFighter.Opponent == null)
            {
                MoveX = 0f;
                IsBlocking = false;
                return;
            }

            FighterController enemy = selfFighter.Opponent;
            if (selfFighter.CurrentState == FighterState.Dead || enemy.CurrentState == FighterState.Dead)
            {
                MoveX = 0f;
                IsBlocking = false;
                return;
            }

            float dt = Time.deltaTime;
            float dx = enemy.transform.position.x - selfFighter.transform.position.x;
            float dist = Mathf.Abs(dx);
            float dirToEnemy = dx >= 0f ? 1f : -1f;

            // Chain Jab -> Roundhouse Kick combo automatically when active
            if (pendingKickCombo
                && selfFighter.CurrentState == FighterState.Attacking
                && selfFighter.CurrentAttackType == AttackType.Jab)
            {
                if (selfFighter.HasLandedCurrentAttack && dist <= kickRange)
                {
                    queuedKick = true;
                    pendingKickCombo = false;
                }
                return;
            }
            else if (selfFighter.CurrentState != FighterState.Attacking)
            {
                pendingKickCombo = false;
            }

            // 1. Reactive Center Block when opponent attacks within threat range
            if (enemy.CurrentState == FighterState.Attacking && dist <= threatBlockRange)
            {
                if (!evaluatedCurrentEnemyAttack)
                {
                    evaluatedCurrentEnemyAttack = true;
                    if (Random.value <= blockChance)
                    {
                        blockHoldTimer = Random.Range(0.32f, 0.56f);
                    }
                }
            }
            else
            {
                evaluatedCurrentEnemyAttack = false;
            }

            if (blockHoldTimer > 0f)
            {
                blockHoldTimer -= dt;
                IsBlocking = true;
                MoveX = 0f;
                return;
            }

            IsBlocking = false;

            // 2. Respect enemy knockdown: keep good spacing while waiting for Getting Up
            if (enemy.CurrentState == FighterState.KnockedDown)
            {
                MoveX = dist < idealSpacing + 0.45f ? -dirToEnemy : 0f;
                return;
            }

            // 3. Tactical retreat timer
            if (retreatTimer > 0f)
            {
                retreatTimer -= dt;
                MoveX = -dirToEnemy;
                return;
            }

            // 4. Advance if outside Roundhouse Kick range
            if (dist > kickRange)
            {
                MoveX = dirToEnemy;
                return;
            }

            // 5. Inside combat zone: weighted tactical decisions
            decisionCooldown -= dt;
            if (decisionCooldown > 0f)
            {
                if (dist > idealSpacing + 0.25f)
                {
                    MoveX = dirToEnemy;
                }
                else if (dist < punchRange * 0.72f)
                {
                    MoveX = -dirToEnemy;
                }
                else
                {
                    MoveX = 0f;
                }
                return;
            }

            decisionCooldown = Random.Range(minDecisionInterval, maxDecisionInterval);
            MoveX = 0f;

            float roll = Random.value;

            if (dist <= punchRange)
            {
                // Close range: Jab Cross (with Jab -> Roundhouse Kick combo), standalone Roundhouse Kick, or Retreat
                if (roll < aggression * 0.62f)
                {
                    queuedJab = true;
                    pendingKickCombo = Random.value <= comboFollowUpChance;
                }
                else if (roll < aggression * 0.90f)
                {
                    queuedKick = true;
                }
                else
                {
                    retreatTimer = Random.Range(0.24f, 0.46f);
                }
            }
            else
            {
                // Mid range (punchRange..kickRange): Roundhouse Kick poke, step forward, brief Center Block, or retreat
                if (roll < aggression * 0.62f)
                {
                    queuedKick = true;
                }
                else if (roll < aggression * 0.88f)
                {
                    MoveX = dirToEnemy;
                }
                else if (roll < 0.95f)
                {
                    blockHoldTimer = Random.Range(0.22f, 0.40f);
                }
                else
                {
                    retreatTimer = Random.Range(0.22f, 0.38f);
                }
            }
        }
    }
}
