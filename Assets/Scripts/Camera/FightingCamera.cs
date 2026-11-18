using UnityEngine;

namespace FightingGame
{
    /// <summary>
    /// 2.5D side-view fighting game camera.
    /// Smoothly tracks the midpoint between both fighters, zooms Z distance dynamically
    /// based on fighter separation, and supports subtle hit-impact camera shake.
    /// </summary>
    public class FightingCamera : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Transform fighter1;
        [SerializeField] private Transform fighter2;

        [Header("Framing")]
        [SerializeField] private float heightOffset = 1.65f;
        [SerializeField] private float lookAtHeightOffset = 1.25f;
        [SerializeField] private float minDistanceZ = 4.6f;
        [SerializeField] private float maxDistanceZ = 8.4f;
        [SerializeField] private float distancePadding = 2.6f;
        [SerializeField] private float distanceScale = 0.85f;
        [SerializeField] private float maxHorizontalX = 4.8f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.14f;
        [SerializeField] private float rotationSmoothSpeed = 8f;

        private Vector3 smoothVelocity;
        private float shakeTimer;
        private float shakeDuration;
        private float shakeIntensity;

        public void SetTargets(Transform p1, Transform p2)
        {
            fighter1 = p1;
            fighter2 = p2;
        }

        public void TriggerShake(float intensity, float duration)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
            shakeDuration = Mathf.Max(duration, 0.01f);
            shakeTimer = shakeDuration;
        }

        private void LateUpdate()
        {
            if (fighter1 == null || fighter2 == null)
            {
                return;
            }

            Vector3 p1Pos = fighter1.position;
            Vector3 p2Pos = fighter2.position;

            float midX = Mathf.Clamp((p1Pos.x + p2Pos.x) * 0.5f, -maxHorizontalX, maxHorizontalX);
            float separation = Mathf.Abs(p1Pos.x - p2Pos.x);
            float desiredDistance = Mathf.Clamp(distancePadding + separation * distanceScale, minDistanceZ, maxDistanceZ);

            Vector3 targetPos = new Vector3(midX, heightOffset, -desiredDistance);
            Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, targetPos, ref smoothVelocity, positionSmoothTime);

            Vector3 shakeOffset = Vector3.zero;
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float damp = Mathf.Clamp01(shakeTimer / shakeDuration);
                shakeOffset = Random.insideUnitSphere * (shakeIntensity * damp);
                shakeOffset.z = 0f;

                if (shakeTimer <= 0f)
                {
                    shakeIntensity = 0f;
                }
            }

            transform.position = smoothedPos + shakeOffset;

            Vector3 lookTarget = new Vector3(midX, lookAtHeightOffset, 0f);
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - smoothedPos, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothSpeed);
        }
    }
}
