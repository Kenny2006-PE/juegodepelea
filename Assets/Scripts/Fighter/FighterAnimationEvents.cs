using UnityEngine;

namespace FightingGame
{
    /// <summary>
    /// Placed on the GameObject holding the Animator component so Mixamo/Unity Animation Events
    /// are cleanly forwarded to the parent FighterController.
    /// </summary>
    public class FighterAnimationEvents : MonoBehaviour
    {
        private FighterController fighter;

        private void Awake()
        {
            fighter = GetComponentInParent<FighterController>();
        }

        public void AnimEvent_EnableHitbox()
        {
            EnsureFighter();
            fighter?.OnAnimEventEnableHitbox();
        }

        public void AnimEvent_DisableHitbox()
        {
            EnsureFighter();
            fighter?.OnAnimEventDisableHitbox();
        }

        public void AnimEvent_AttackEnd()
        {
            EnsureFighter();
            fighter?.OnAnimEventAttackEnd();
        }

        private void EnsureFighter()
        {
            if (fighter == null)
            {
                fighter = GetComponentInParent<FighterController>();
            }
        }
    }
}
