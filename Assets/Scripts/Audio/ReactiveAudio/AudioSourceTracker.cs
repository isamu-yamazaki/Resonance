using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Audio
{
    public class AudioSourceTracker : MonoBehaviour
    {
        public static AudioSourceTracker Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float defaultDuration = 3f;
        [SerializeField] private float propagationSpeed = 50f;
        [SerializeField] private float baseWaveDistance = 150f;

        private List<AudioSourceData> activeSources = new List<AudioSourceData>();
        private readonly System.Predicate<AudioSourceData> isExpired = source => source.IsExpired();

        public float BaseWaveDistance => baseWaveDistance;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Update()
        {
            activeSources.RemoveAll(isExpired);
        }

        public void RegisterSound(Vector3 position, float duration = -1f)
        {
            if (duration < 0f)
                duration = defaultDuration;

            float intensity = AudioBusMonitor.Instance != null
                ? AudioBusMonitor.Instance.GetMaxBusIntensity()
                : 0f;

            activeSources.Add(new AudioSourceData(position, duration, intensity));
        }

        public AudioSourceData FindLoudestNearby(Vector3 position, float searchRadius)
        {
            AudioSourceData loudest = null;
            float maxWeightedIntensity = 0f;

            foreach (var source in activeSources)
            {
                float distance = Vector3.Distance(position, source.Position);
                float waveMaxDistance = baseWaveDistance * source.PeakIntensity;
                float waveRadius = source.GetAge() * propagationSpeed;

                if (distance > waveRadius) continue;
                if (distance > waveMaxDistance) continue;
                if (distance > searchRadius) continue;

                float intensity = source.GetCurrentIntensity();
                float distanceAttenuation = Mathf.Clamp01(1f - (distance / waveMaxDistance));
                float weightedIntensity = intensity * distanceAttenuation;

                if (weightedIntensity > maxWeightedIntensity)
                {
                    maxWeightedIntensity = weightedIntensity;
                    loudest = source;
                }
            }

            return loudest;
        }

        void OnDrawGizmos()
        {
            if (activeSources == null) return;

            foreach (var source in activeSources)
            {
                float intensity = source.GetCurrentIntensity();

                Gizmos.color = Color.yellow * intensity;
                Gizmos.DrawWireSphere(source.Position, 0.5f);

                float waveRadius = source.GetAge() * propagationSpeed;
                float waveMaxDistance = baseWaveDistance * source.PeakIntensity;

                if (waveRadius < waveMaxDistance)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, intensity * 0.3f);
                    Gizmos.DrawWireSphere(source.Position, waveRadius);
                }
            }
        }
    }
}
