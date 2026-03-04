using UnityEngine;

/// <summary>
/// Wwise Ambience Manager - Procedural Ambience Blending
/// Automatically blends outdoor/indoor ambience based on WwiseSmartReverb data.
/// No manual trigger zones!
/// </summary>
[RequireComponent(typeof(AkGameObj))]
public class WwiseAmbienceManager : MonoBehaviour
{
    [Header("Wwise Event")]
    [Tooltip("Ambience event (should have indoor/outdoor blend parameter)")]
    public AK.Wwise.Event ambienceEvent;

    [Header("Scanner Reference")]
    [Tooltip("WwiseSmartReverb component (usually on player/listener)")]
    public WwiseSmartReverb scannerSource;

    [Header("Mix Parameter")]
    [Tooltip("RTPC name for ambience mix (0 = outdoor, 100 = indoor)")]
    public string mixParameterName = "AmbienceMix";

    [Header("Thresholds")]
    [Tooltip("Enclosure value for outdoor")]
    [Range(0f, 1f)]
    public float outdoorThreshold = 0.1f;
    
    [Tooltip("Enclosure value for indoor")]
    [Range(0f, 1f)]
    public float indoorThreshold = 0.7f;

    [Header("Blend Curve")]
    [Tooltip("Maps enclosure to mix value")]
    public AnimationCurve mixCurve = AnimationCurve.Linear(0f, 0f, 1f, 100f);

    [Header("Settings")]
    [Tooltip("Transition speed")]
    [Range(0.1f, 5f)]
    public float transitionSpeed = 1f;
    
    [Tooltip("Auto-start on scene load")]
    public bool autoStart = true;

    // Internal
    private uint playingID;
    private bool isPlaying;
    private float currentMix;
    private float targetMix;

    void Start()
    {
        if (scannerSource == null)
        {
            scannerSource = FindAnyObjectByType<WwiseSmartReverb>();
            if (scannerSource == null)
            {
                Debug.LogError("[WwiseAmbienceManager] No WwiseSmartReverb found in scene!");
                enabled = false;
                return;
            }
        }

        if (autoStart)
        {
            StartAmbience();
        }
    }

    void Update()
    {
        if (!isPlaying || scannerSource == null)
            return;

        // Get enclosure from scanner
        float enclosure = scannerSource.EnclosureFactor;

        // Map to mix value
        float normalizedEnclosure = Mathf.InverseLerp(outdoorThreshold, indoorThreshold, enclosure);
        targetMix = mixCurve.Evaluate(normalizedEnclosure);

        // Smooth transition
        currentMix = Mathf.Lerp(currentMix, targetMix, Time.deltaTime * transitionSpeed);

        // Update Wwise
        AkUnitySoundEngine.SetRTPCValue(mixParameterName, currentMix, gameObject);
    }

    public void StartAmbience()
    {
        if (isPlaying)
            return;

        if (ambienceEvent != null && ambienceEvent.IsValid())
        {
            playingID = ambienceEvent.Post(gameObject);
            isPlaying = true;
        }
    }

    public void StopAmbience(float fadeTime = 1f)
    {
        if (!isPlaying)
            return;

        if (playingID != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Stop,
                playingID,
                (int)(fadeTime * 1000),
                AkCurveInterpolation.AkCurveInterpolation_Linear
            );
        }

        isPlaying = false;
    }

    void OnDisable()
    {
        if (isPlaying)
        {
            StopAmbience(0.5f);
        }
    }
}