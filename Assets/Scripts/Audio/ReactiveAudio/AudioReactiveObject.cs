using UnityEngine;

namespace Resonance.Audio
{
    // Fully client-side. Polls local AudioSourceTracker every frame and runs ADSR locally.
    // No networking — all clients receive the same sound broadcast and converge on the same result.
    public class AudioReactiveObject : MonoBehaviour
    {
        [Header("Material Settings")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color emissionColor = Color.cyan;
        [SerializeField] private float emissionIntensity = 5f;

        [Header("Audio Feedback")]
        [SerializeField] private bool enableAudioFeedback = true;

        [Header("Envelope (ADSR)")]
        [SerializeField] private float attackSpeed = 30f;
        [SerializeField] private float sustainTime = 1f;
        [SerializeField] private float releaseSpeed = 0.5f;

        [Header("Threshold")]
        [SerializeField] private float threshold = 0.05f;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private Material materialInstance;
        private float currentIntensity = 0f;
        private float targetIntensity = 0f;
        private float externalIntensity = 0f;
        private float peakIntensity = 0f;
        private float sustainTimer = 0f;
        private bool inSustain = false;
        private bool isFeedbackPlaying = false;

        private void Start()
        {
            SetupMaterial();
        }

        private void Update()
        {
#if !UNITY_SERVER
            CalculateAudioState();
            ApplyEmission();

            if (enableAudioFeedback)
                UpdateAudioFeedback(currentIntensity);
#endif
        }

        private void CalculateAudioState()
        {
            if (AudioSourceTracker.Instance != null)
            {
                AudioSourceData nearestSource = AudioSourceTracker.Instance.FindLoudestNearby(
                    transform.position,
                    AudioSourceTracker.Instance.BaseWaveDistance
                );

                if (nearestSource != null)
                {
                    float distance = Vector3.Distance(transform.position, nearestSource.Position);
                    float sourceIntensity = nearestSource.GetCurrentIntensity();
                    float waveMaxDistance = AudioSourceTracker.Instance.BaseWaveDistance * nearestSource.PeakIntensity;
                    float distanceAttenuation = 1f - Mathf.Clamp01(distance / waveMaxDistance);
                    targetIntensity = Mathf.Clamp01(sourceIntensity * distanceAttenuation);
                }
                else
                {
                    targetIntensity = 0f;
                }
            }
            else
            {
                targetIntensity = 0f;
            }

            if (targetIntensity < threshold)
                targetIntensity = 0f;

            targetIntensity = Mathf.Max(targetIntensity, externalIntensity);

            // ADSR Envelope
            if (targetIntensity > currentIntensity)
            {
                currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * attackSpeed);

                if (currentIntensity > peakIntensity)
                {
                    peakIntensity = currentIntensity;
                    sustainTimer = sustainTime;
                    inSustain = true;
                }
            }
            else if (inSustain && sustainTimer > 0f)
            {
                currentIntensity = peakIntensity;
                sustainTimer -= Time.deltaTime;

                if (sustainTimer <= 0f)
                    inSustain = false;
            }
            else
            {
                currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * releaseSpeed);

                if (currentIntensity < 0.01f)
                    peakIntensity = 0f;
            }

            if (float.IsNaN(currentIntensity))
                currentIntensity = 0f;

            if (debugLog)
                Debug.Log($"[AudioReactiveObject] Target: {targetIntensity:F3}, Current: {currentIntensity:F3}, Sustain: {sustainTimer:F2}s");
        }

        private void ApplyEmission()
        {
            if (materialInstance == null) return;
            Color finalEmission = emissionColor * (currentIntensity * emissionIntensity);
            materialInstance.SetColor("_EmissionColor", finalEmission);
        }

        private void UpdateAudioFeedback(float intensity)
        {
            bool shouldPlay = intensity > 0f;

            if (shouldPlay && !isFeedbackPlaying)
                StartAudioFeedback();
            else if (!shouldPlay && isFeedbackPlaying)
                StopAudioFeedback();

#if !UNITY_SERVER
            if (isFeedbackPlaying)
            {
                float volumeValue = Mathf.Clamp01(intensity) * 100f;
                AkUnitySoundEngine.SetRTPCValue("Reactive_Feedback_Volume", volumeValue, gameObject);
            }
#endif
        }

        private void StartAudioFeedback()
        {
#if !UNITY_SERVER
            AkUnitySoundEngine.PostEvent("Play_Reactive_Feedback", gameObject);
#endif
            isFeedbackPlaying = true;
        }

        private void StopAudioFeedback()
        {
#if !UNITY_SERVER
            AkUnitySoundEngine.PostEvent("Stop_Reactive_Feedback", gameObject);
#endif
            isFeedbackPlaying = false;
        }

        public void SetExternalIntensity(float intensity)
        {
            externalIntensity = Mathf.Clamp01(intensity);
        }

        private void SetupMaterial()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            if (targetRenderer != null)
            {
                materialInstance = targetRenderer.material;
                materialInstance.EnableKeyword("_EMISSION");
                materialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                Debug.LogError($"[AudioReactiveObject] No Renderer found on {gameObject.name}!");
            }
        }

        private void OnDestroy()
        {
            if (materialInstance != null)
                Destroy(materialInstance);

            if (isFeedbackPlaying)
                StopAudioFeedback();
        }
    }
}
