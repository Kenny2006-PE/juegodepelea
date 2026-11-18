using UnityEngine;
using UnityEngine.EventSystems;

namespace FightingGame
{
    /// <summary>
    /// Adds crisp hover and press scale feedback to UI buttons for a responsive arcade feel.
    /// </summary>
    public class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.045f;
        [SerializeField] private float pressScale = 0.96f;
        [SerializeField] private float smoothSpeed = 16f;
        [SerializeField] private bool idlePulse;
        [SerializeField] private float pulseAmplitude = 0.02f;
        [SerializeField] private float pulseSpeed = 3.2f;

        private Vector3 baseScale = Vector3.one;
        private bool isHovered;
        private bool isPressed;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            isHovered = false;
            isPressed = false;
            transform.localScale = baseScale;
        }

        public void SetIdlePulse(bool enabled)
        {
            idlePulse = enabled;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
        }

        private void Update()
        {
            float targetFactor = 1f;
            if (isPressed)
            {
                targetFactor = pressScale;
            }
            else if (isHovered)
            {
                targetFactor = hoverScale;
            }
            else if (idlePulse)
            {
                targetFactor = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
            }

            Vector3 target = baseScale * targetFactor;
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * smoothSpeed);
        }
    }
}
