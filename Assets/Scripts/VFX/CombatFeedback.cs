using UnityEngine;

namespace FightingGame
{
    /// <summary>
    /// Provides visual hit/block spark bursts and procedural audio feedback out of the box,
    /// with optional inspector overrides if custom AudioClips are assigned later.
    /// </summary>
    public class CombatFeedback : MonoBehaviour
    {
        [Header("Optional Custom Audio Clips (Procedural Fallback Used If Empty)")]
        [SerializeField] private AudioClip lightHitClip;
        [SerializeField] private AudioClip heavyHitClip;
        [SerializeField] private AudioClip kickHitClip;
        [SerializeField] private AudioClip blockClip;
        [SerializeField] private AudioClip koClip;
        [SerializeField] private AudioClip roundBellClip;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.75f;

        private AudioSource audioSource;
        private Material particleMaterial;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            GenerateFallbackClips();
            CreateParticleMaterial();
        }

        public void PlayHitFeedback(Vector3 worldPos, AttackType attackType, bool wasBlocked)
        {
            if (wasBlocked)
            {
                SpawnBurst(worldPos, new Color(0.25f, 0.85f, 1f, 1f), 18, 3.5f, 0.22f);
                PlayClip(blockClip, 0.9f);
                return;
            }

            switch (attackType)
            {
                case AttackType.Jab:
                    SpawnBurst(worldPos, new Color(1f, 0.82f, 0.25f, 1f), 14, 3.8f, 0.20f);
                    PlayClip(lightHitClip, 0.85f);
                    break;

                case AttackType.Kick:
                    SpawnBurst(worldPos, new Color(1f, 0.22f, 0.14f, 1f), 32, 6.5f, 0.34f);
                    PlayClip(heavyHitClip != null ? heavyHitClip : kickHitClip, 1f);
                    break;
            }
        }

        public void PlayKOFeedback(Vector3 worldPos)
        {
            SpawnBurst(worldPos + Vector3.up * 1.1f, new Color(1f, 0.85f, 0.15f, 1f), 45, 7.5f, 0.55f);
            PlayClip(koClip, 1f);
        }

        public void PlayRoundBell()
        {
            PlayClip(roundBellClip, 0.8f);
        }

        private void PlayClip(AudioClip clip, float pitchVariance)
        {
            if (audioSource == null || clip == null) return;
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, sfxVolume * pitchVariance);
        }

        private void SpawnBurst(Vector3 position, Color color, int count, float speed, float lifetime)
        {
            GameObject fxObj = new GameObject("CombatSparkVFX");
            fxObj.SetActive(false);
            fxObj.transform.position = position;

            ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.duration = lifetime;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.55f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.6f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            ParticleSystem.SizeOverLifetimeModule sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ParticleSystemRenderer renderer = fxObj.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && particleMaterial != null)
            {
                renderer.sharedMaterial = particleMaterial;
            }

            fxObj.SetActive(true);
            ps.Play();
            Destroy(fxObj, lifetime + 0.35f);
        }

        private void CreateParticleMaterial()
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                particleMaterial = new Material(shader);
                particleMaterial.color = Color.white;
            }
        }

        private void GenerateFallbackClips()
        {
            if (lightHitClip == null) lightHitClip = CreateImpactClip("SFX_LightHit", 0.11f, 220f, 70f, 0.55f);
            if (kickHitClip == null) kickHitClip = CreateImpactClip("SFX_KickHit", 0.16f, 175f, 52f, 0.72f);
            if (heavyHitClip == null) heavyHitClip = CreateImpactClip("SFX_HeavyHit", 0.24f, 130f, 38f, 0.85f);
            if (blockClip == null) blockClip = CreateMetallicBlockClip("SFX_Block", 0.15f);
            if (koClip == null) koClip = CreateImpactClip("SFX_KO", 0.45f, 110f, 28f, 0.90f);
            if (roundBellClip == null) roundBellClip = CreateBellClip("SFX_Bell", 0.48f);
        }

        private static AudioClip CreateImpactClip(string name, float duration, float startFreq, float endFreq, float noiseMix)
        {
            const int sampleRate = 22050;
            int samples = Mathf.Max(1, Mathf.FloorToInt(sampleRate * duration));
            float[] data = new float[samples];
            System.Random rng = new System.Random(name.GetHashCode());

            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Pow(1f - t, 2.2f);
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += (2f * Mathf.PI * freq) / sampleRate;

                float tone = Mathf.Sin(phase);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[i] = Mathf.Clamp((tone * (1f - noiseMix) + noise * noiseMix) * env, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateMetallicBlockClip(string name, float duration)
        {
            const int sampleRate = 22050;
            int samples = Mathf.Max(1, Mathf.FloorToInt(sampleRate * duration));
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 14f);
                float seconds = (float)i / sampleRate;
                float wave = Mathf.Sin(2f * Mathf.PI * 520f * seconds) * 0.5f
                           + Mathf.Sin(2f * Mathf.PI * 1080f * seconds) * 0.35f
                           + Mathf.Sin(2f * Mathf.PI * 1790f * seconds) * 0.15f;
                data[i] = Mathf.Clamp(wave * env, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateBellClip(string name, float duration)
        {
            const int sampleRate = 22050;
            int samples = Mathf.Max(1, Mathf.FloorToInt(sampleRate * duration));
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 5.5f);
                float seconds = (float)i / sampleRate;
                float wave = Mathf.Sin(2f * Mathf.PI * 660f * seconds) * 0.55f
                           + Mathf.Sin(2f * Mathf.PI * 1320f * seconds) * 0.25f
                           + Mathf.Sin(2f * Mathf.PI * 990f * seconds) * 0.20f;
                data[i] = Mathf.Clamp(wave * env, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
