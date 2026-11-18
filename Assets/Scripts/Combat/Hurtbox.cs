using UnityEngine;

namespace FightingGame
{
    /// <summary>
    /// Trigger collider representing a fighter's vulnerable body area.
    /// Forwards incoming hits from enemy Hitboxes to the owning FighterController.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hurtbox : MonoBehaviour
    {
        [SerializeField] private FighterController owner;

        public FighterController Owner => owner;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (owner == null)
            {
                owner = GetComponentInParent<FighterController>();
            }
        }

        public void Configure(FighterController fighterOwner)
        {
            owner = fighterOwner;
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public void ReceiveHit(AttackData attack, FighterController attacker, Vector3 hitPoint)
        {
            if (owner == null)
            {
                return;
            }

            owner.OnReceiveHit(attack, attacker, hitPoint);
        }
    }
}
