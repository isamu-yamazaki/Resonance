using System;
using System.Collections;
using PurrNet;
using UnityEngine;

namespace Resonance.Audio
{
    public class AudioReactiveObject : NetworkBehaviour
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
        private float peakIntensity = 0f;
        private float sustainTimer = 0f;
        private bool inSustain = false;
        private bool isFeedbackPlaying = false;

        [Header("Network Update Intervals")]
        [SerializeField] private float serverToClientPropagationIntervalSeconds = 0.2f;

        /// <summary>
        /// A base control for how often clients emit updates to the server.
        /// If there is a detected sound from the client, that will get reported
        /// immediately outside of this coroutine loop.
        /// Otherwise, `null` sound events are propagated from a loop with this wait time.
        /// </summary>
        [SerializeField] private float clientToServerNullSourceReportingIntervalSeconds = 1f;

        private AudioSourceData clientReportedSource;

        void Start()
        {
            SetupMaterial();
        }

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            if (asServer)
            {
                currentIntensity = 0f;
                ApplyEmissionAndAudioFeedbackForAllClients(0f);
                StartCoroutine(ServerPropagationLoop());
            }

            if (isClient)
            {
                StartCoroutine(ClientReportingLoop());
            }
        }

        protected override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);
            StopAllCoroutines();
        }

        void Update()
        {
            if (isServer)
            {
                CalculateAudioState();
            }

            if (isClient)
            {
                AudioSourceData nearestSource = FindNearestSource();
                if (nearestSource != null)
                {
                    SetNearestAudioSourceOnServer(nearestSource);
                }
            }
        }

        private IEnumerator ServerPropagationLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(serverToClientPropagationIntervalSeconds);
                ApplyEmissionAndAudioFeedbackForAllClients(currentIntensity);
            }
        }

        private IEnumerator ClientReportingLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(clientToServerNullSourceReportingIntervalSeconds);

                if (AudioSourceTracker.Instance == null)
                {
                    Debug.LogWarning("[AudioReactiveObject] AudioSourceTracker not found in scene!");
                    continue;
                }

                AudioSourceData nearestSource = FindNearestSource();
                SetNearestAudioSourceOnServer(nearestSource);
            }
        }

        private AudioSourceData FindNearestSource()
        {
            return AudioSourceTracker.Instance.FindLoudestNearby(
                transform.position,
                AudioSourceTracker.Instance.BaseWaveDistance
            );
        }

        [ServerRpc(PurrNet.Transports.Channel.ReliableOrdered, requireOwnership: false)]
        public void SetNearestAudioSourceOnServer(AudioSourceData source)
        {
            clientReportedSource = source;
        }

        [ServerRpc(PurrNet.Transports.Channel.ReliableOrdered, requireOwnership: false)]
        private void CalculateAudioState()
        {
            if (clientReportedSource != null)
            {
                float distance = Vector3.Distance(transform.position, clientReportedSource.Position);
                float sourceIntensity = clientReportedSource.GetCurrentIntensity();
                float waveMaxDistance = AudioSourceTracker.Instance.BaseWaveDistance * clientReportedSource.PeakIntensity;
                float distanceAttenuation = 1f - Mathf.Clamp01(distance / waveMaxDistance);

                targetIntensity = sourceIntensity * distanceAttenuation;
            }
            else
            {
                targetIntensity = 0f;
            }

            if (targetIntensity < threshold)
            {
                targetIntensity = 0f;
            }

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
                {
                    inSustain = false;
                }
            }
            else
            {
                currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * releaseSpeed);

                if (currentIntensity < 0.01f)
                {
                    peakIntensity = 0f;
                }
            }

            // as a fallback for zero-division results
            if (float.IsNaN(currentIntensity))
            {
                currentIntensity = 0f;
            }

            if (debugLog)
            {
                Debug.Log($"[AudioReactiveObject] Target: {targetIntensity:F3}, Current: {currentIntensity:F3}, Sustain: {sustainTimer:F2}s");
            }
        }

        [ServerRpc(PurrNet.Transports.Channel.ReliableOrdered, requireOwnership: false)]
        private void ResetTargetIntensity()
        {
            targetIntensity = 0f;
        }

        private void UpdateAudioFeedback(float intensity)
        {
            bool shouldPlay = intensity > 0f;

            if (shouldPlay && !isFeedbackPlaying)
            {
                StartAudioFeedback();
            }
            else if (!shouldPlay && isFeedbackPlaying)
            {
                StopAudioFeedback();
            }

            if (isFeedbackPlaying)
            {
                float volumeValue = Mathf.Clamp01(intensity) * 100f;
                AkSoundEngine.SetRTPCValue("Reactive_Feedback_Volume", volumeValue, gameObject);
            }
        }

        private void StartAudioFeedback()
        {
            AkUnitySoundEngine.PostEvent("Play_Reactive_Feedback", gameObject);
            isFeedbackPlaying = true;
        }

        private void StopAudioFeedback()
        {
            AkUnitySoundEngine.PostEvent("Stop_Reactive_Feedback", gameObject);
            isFeedbackPlaying = false;
        }

        private void SetupMaterial()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

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

        [ObserversRpc(PurrNet.Transports.Channel.ReliableOrdered)]
        private void ApplyEmissionAndAudioFeedbackForAllClients(float intensity)
        {
            if (materialInstance == null) return;

            Color finalEmission = emissionColor * (intensity * emissionIntensity);
            materialInstance.SetColor("_EmissionColor", finalEmission);

            if (enableAudioFeedback)
            {
                UpdateAudioFeedback(intensity);
            }
        }

        void OnDestroy()
        {
            if (materialInstance != null)
            {
                Destroy(materialInstance);
            }

            if (isFeedbackPlaying)
            {
                StopAudioFeedback();
            }
        }
    }
}
