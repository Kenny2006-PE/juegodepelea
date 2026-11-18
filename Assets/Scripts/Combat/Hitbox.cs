using UnityEngine;

namespace FightingGame
{
    /// <summary>
    /// Trigger collider attached to a Humanoid hand or foot bone.
    /// Only detects hits while activated during the active frames of an attack.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] private FighterController owner;
        [SerializeField] private HitboxLimb limbType = HitboxLimb.RightHand;

        private Collider triggerCollider;
        private bool isActive;
        private AttackData currentAttack;

        public HitboxLimb LimbType => limbType;
        public bool IsActive => isActive;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = false;

            if (owner == null)
            {
                owner = GetComponentInParent<FighterController>();
            }
        }

        public void Configure(FighterController fighterOwner, HitboxLimb limb)
        {
            owner = fighterOwner;
            limbType = limb;

            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider>();
            }

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.enabled = false;
            }
        }

        public void Activate(AttackData attack)
        {
            currentAttack = attack;
            isActive = true;

            if (triggerCollider != null)
            {
                triggerCollider.enabled = true;
            }
        }

        public void Deactivate()
        {
            isActive = false;

            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryRegisterHit(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryRegisterHit(other);
        }

        private void TryRegisterHit(Collider other)
        {
            if (!isActive || owner == null)
            {
                return;
            }

            Hurtbox hurtbox = other.GetComponent<Hurtbox>();
            if (hurtbox == null)
            {
                hurtbox = other.GetComponentInParent<Hurtbox>();
            }

            if (hurtbox == null || hurtbox.Owner == null || hurtbox.Owner == owner)
            {
                return;
            }

            if (!owner.TryClaimHitTarget(hurtbox.Owner))
            {
                return;
            }

            Vector3 contactPoint = other.ClosestPoint(transform.position);
            hurtbox.ReceiveHit(currentAttack, owner, contactPoint);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Collider col = triggerCollider != null ? triggerCollider : GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = isActive
                ? new Color(1f, 0.15f, 0.15f, 0.65f)
                : new Color(1f, 0.85f, 0.1f, 0.18f);

            if (col is SphereCollider sphere)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else
            {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
#endif
    }
}
