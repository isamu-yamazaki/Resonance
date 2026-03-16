using UnityEngine;

public class WwiseDirectionalAmbience : MonoBehaviour
{
    [Header("Wwise Events")]
    public AK.Wwise.Event outdoorAmbienceEvent;
    public AK.Wwise.Event indoorAmbienceEvent;

    [Header("Scanner Reference")]
    public WwiseSmartReverb scannerSource;

    [Header("Directional Detection")]
    public int directionCount = 4;
    public float rayDistance = 30f;
    public float headHeight = 1.6f;
    public LayerMask environmentLayer;

    [Header("Emitter Settings")]
    public float emitterDistance = 15f;
    [Range(0f, 1f)] public float activationThreshold = 0.3f;

    [Header("Update Rate")]
    [Range(1f, 10f)] public float scanRate = 5f;

    [Header("Settings")]
    public bool autoStart = true;

    [Header("Debug")]
    public bool drawDebugRays = false;
    public bool debugLog = false;

    private GameObject[] outdoorEmitters;
    private uint[] outdoorPlayingIDs;
    private uint indoorPlayingID;
    private bool isPlaying;
    private float lastScanTime;
    private Vector3[] directions;
    private float globalEnclosure;

    void Start()
    {
        if (scannerSource == null)
        {
            scannerSource = FindAnyObjectByType<WwiseSmartReverb>();
            if (scannerSource == null)
            {
                Debug.LogError("[DirectionalAmbience] No WwiseSmartReverb found!");
                enabled = false;
                return;
            }
        }

        InitializeDirections();
        CreateEmitters();
        lastScanTime = -1f;

        if (autoStart)
        {
            StartAmbience();
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        globalEnclosure = scannerSource != null ? scannerSource.EnclosureFactor : 0f;

        if (Time.time - lastScanTime >= 1f / scanRate)
        {
            UpdateDirectionalEmitters();
            lastScanTime = Time.time;
        }

        UpdateIndoorVolume();
    }

    void InitializeDirections()
    {
        directions = new Vector3[directionCount];
        float angleStep = 360f / directionCount;

        for (int i = 0; i < directionCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            directions[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }

    void CreateEmitters()
    {
        outdoorEmitters = new GameObject[directionCount];
        outdoorPlayingIDs = new uint[directionCount];

        for (int i = 0; i < directionCount; i++)
        {
            GameObject emitter = new GameObject($"OutdoorEmitter_{i}");
            emitter.transform.SetParent(transform);
            emitter.AddComponent<AkGameObj>();
            outdoorEmitters[i] = emitter;
            outdoorPlayingIDs[i] = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        }
    }

    void UpdateDirectionalEmitters()
    {
        Vector3 listenerPos = transform.position;
        Vector3 headPos = listenerPos + Vector3.up * headHeight;

        int activeCount = 0;
        float[] opennessValues = new float[directionCount];

        for (int i = 0; i < directionCount; i++)
        {
            Vector3 worldDir = directions[i];
            float openness = CalculateOpenness(headPos, worldDir);
            opennessValues[i] = openness;

            if (openness > activationThreshold)
            {
                activeCount++;
            }
        }

        float volumeCompensation = activeCount > 0 ? -6f * Mathf.Log10(activeCount) : 0f;

        for (int i = 0; i < directionCount; i++)
        {
            Vector3 worldDir = directions[i];
            float openness = opennessValues[i];

            Vector3 emitterPos = listenerPos + worldDir * emitterDistance;
            outdoorEmitters[i].transform.position = emitterPos;

            if (openness > activationThreshold)
            {
                if (outdoorPlayingIDs[i] == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
                {
                    if (outdoorAmbienceEvent != null && outdoorAmbienceEvent.IsValid())
                    {
                        outdoorPlayingIDs[i] = outdoorAmbienceEvent.Post(outdoorEmitters[i]);
                    }
                }

                float baseVolume = Mathf.Lerp(-96f, 0f, openness);
                float compensatedVolume = baseVolume + volumeCompensation;
                AkSoundEngine.SetRTPCValue("DirectionalAmbienceVolume", compensatedVolume, outdoorEmitters[i]);
            }
            else
            {
                if (outdoorPlayingIDs[i] != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
                {
                    AkUnitySoundEngine.ExecuteActionOnPlayingID(
                        AkActionOnEventType.AkActionOnEventType_Stop,
                        outdoorPlayingIDs[i],
                        500,
                        AkCurveInterpolation.AkCurveInterpolation_Linear
                    );
                    outdoorPlayingIDs[i] = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
                }
            }

            if (debugLog)
            {
                Debug.Log($"[DirectionalAmbience] Dir {i}: Openness {openness:F2}");
            }
        }
    }

    float CalculateOpenness(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        bool didHit = Physics.Raycast(origin, direction, out hit, rayDistance, environmentLayer);

        if (drawDebugRays)
        {
            Color rayColor = didHit ? Color.red : Color.green;
            Vector3 endPoint = didHit ? hit.point : origin + direction * rayDistance;
            Debug.DrawLine(origin, endPoint, rayColor, 1f / scanRate);
        }

        if (!didHit)
        {
            return 1f;
        }

        float normalizedDist = hit.distance / rayDistance;
        return normalizedDist;
    }

    void UpdateIndoorVolume()
    {
        if (indoorPlayingID == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            if (debugLog)
            {
                Debug.LogWarning("[DirectionalAmbience] Indoor ambience not playing!");
            }
            return;
        }

        float indoorVolume = Mathf.Lerp(-96f, 0f, globalEnclosure);
        AkSoundEngine.SetRTPCValue("IndoorAmbienceVolume", indoorVolume, gameObject);

        if (debugLog)
        {
            Debug.Log($"[DirectionalAmbience] Indoor volume RTPC: {indoorVolume:F1} (enclosure: {globalEnclosure:F2})");
        }
    }

    public void StartAmbience()
    {
        if (isPlaying) return;

        if (indoorAmbienceEvent != null && indoorAmbienceEvent.IsValid())
        {
            indoorPlayingID = indoorAmbienceEvent.Post(gameObject);
            if (debugLog)
            {
                Debug.Log($"[DirectionalAmbience] Started indoor ambience, playingID: {indoorPlayingID}");
            }
        }
        else
        {
            Debug.LogWarning("[DirectionalAmbience] Indoor ambience event not assigned or invalid!");
        }

        isPlaying = true;
    }

    public void StopAmbience()
    {
        if (!isPlaying) return;

        for (int i = 0; i < outdoorPlayingIDs.Length; i++)
        {
            if (outdoorPlayingIDs[i] != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
            {
                AkUnitySoundEngine.ExecuteActionOnPlayingID(
                    AkActionOnEventType.AkActionOnEventType_Stop,
                    outdoorPlayingIDs[i],
                    500,
                    AkCurveInterpolation.AkCurveInterpolation_Linear
                );
                outdoorPlayingIDs[i] = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
            }
        }

        if (indoorPlayingID != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Stop,
                indoorPlayingID,
                500,
                AkCurveInterpolation.AkCurveInterpolation_Linear
            );
            indoorPlayingID = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        }

        isPlaying = false;
    }

    void OnDisable()
    {
        if (isPlaying)
        {
            StopAmbience();
        }
    }

    void OnDestroy()
    {
        if (outdoorEmitters != null)
        {
            foreach (var emitter in outdoorEmitters)
            {
                if (emitter != null)
                {
                    Destroy(emitter);
                }
            }
        }
    }
}
