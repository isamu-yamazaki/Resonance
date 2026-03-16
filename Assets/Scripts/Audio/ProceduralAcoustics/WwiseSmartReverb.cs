using UnityEngine;

/// <summary>
/// Wwise Smart Reverb - Volumetric Raycasting for Room Acoustics
/// Uses Fibonacci Sphere Sampling to calculate room parameters procedurally.
/// No manual reverb zones required!
/// </summary>
public class WwiseSmartReverb : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("If true, sets Global RTPCs. If false, sets RTPCs on target emitter")]
    public bool isGlobal = true;
    
    [Tooltip("Target game object for local mode (leave null for global)")]
    public GameObject targetEmitter;

    [Header("Wwise Parameters")]
    [Tooltip("RTPC name for enclosure (0 = outdoor, 1 = indoor)")]
    public string enclosureParameter = "Enclosure";
    
    [Tooltip("RTPC name for room size (meters)")]
    public string roomSizeParameter = "RoomSize";

    [Header("Raycast Settings")]
    [Tooltip("Number of rays in Fibonacci sphere")]
    [Range(10, 60)]
    public int raysCount = 30;
    
    [Tooltip("Maximum ray distance")]
    public float maxDistance = 50f;
    
    [Tooltip("Environment layer mask")]
    public LayerMask environmentLayer;

    [Header("Update Rate")]
    [Tooltip("Scans per second (Hz)")]
    [Range(1f, 30f)]
    public float scanRate = 5f;

    [Header("Calibration")]
    [Tooltip("Maps hit ratio to enclosure")]
    public AnimationCurve enclosureCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Maps average distance to room size")]
    public AnimationCurve roomSizeCurve = AnimationCurve.Linear(0f, 5f, 50f, 100f);

    [Header("Debug")]
    public bool drawRays = false;
    public Color hitColor = Color.red;
    public Color missColor = Color.green;

    // Internal
    private Vector3[] rayDirections;
    private float lastScanTime;
    private float currentEnclosure;
    private float currentRoomSize;
    private float targetEnclosure;
    private float targetRoomSize;
    private const float smoothingSpeed = 5f;

    // Public accessors
    public float EnclosureFactor => currentEnclosure;
    public float RoomSize => currentRoomSize;

    void Start()
    {
        GenerateFibonacciSphere();
        lastScanTime = -1f; // Force immediate scan
    }

    void Update()
    {
        // Time-sliced scanning
        if (Time.time - lastScanTime >= 1f / scanRate)
        {
            PerformScan();
            lastScanTime = Time.time;
        }

        // Smooth parameter updates
        SmoothParameters();
    }

    void GenerateFibonacciSphere()
    {
        rayDirections = new Vector3[raysCount];
        float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;
        float angleIncrement = Mathf.PI * 2f * goldenRatio;

        for (int i = 0; i < raysCount; i++)
        {
            float t = (float)i / raysCount;
            float inclination = Mathf.Acos(1f - 2f * t);
            float azimuth = angleIncrement * i;

            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float z = Mathf.Cos(inclination);

            rayDirections[i] = new Vector3(x, y, z);
        }
    }

    void PerformScan()
    {
        int hitCount = 0;
        float totalDistance = 0f;
        Vector3 origin = transform.position;

        foreach (Vector3 direction in rayDirections)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, maxDistance, environmentLayer))
            {
                hitCount++;
                totalDistance += hit.distance;

                if (drawRays)
                    Debug.DrawLine(origin, hit.point, hitColor, 1f / scanRate);
            }
            else
            {
                totalDistance += maxDistance;

                if (drawRays)
                    Debug.DrawRay(origin, direction * maxDistance, missColor, 1f / scanRate);
            }
        }

        // Calculate acoustic parameters
        float hitRatio = (float)hitCount / raysCount;
        float avgDistance = totalDistance / raysCount;

        targetEnclosure = enclosureCurve.Evaluate(hitRatio);
        targetRoomSize = roomSizeCurve.Evaluate(avgDistance);
    }

    void SmoothParameters()
    {
        currentEnclosure = Mathf.Lerp(currentEnclosure, targetEnclosure, Time.deltaTime * smoothingSpeed);
        currentRoomSize = Mathf.Lerp(currentRoomSize, targetRoomSize, Time.deltaTime * smoothingSpeed);

        UpdateWwiseParameters();
    }

    void UpdateWwiseParameters()
    {
        if (isGlobal)
        {
            // Global RTPCs
            AkUnitySoundEngine.SetRTPCValue(enclosureParameter, currentEnclosure);
            AkUnitySoundEngine.SetRTPCValue(roomSizeParameter, currentRoomSize);
        }
        else if (targetEmitter != null)
        {
            // Local RTPCs
            AkUnitySoundEngine.SetRTPCValue(enclosureParameter, currentEnclosure, targetEmitter);
            AkUnitySoundEngine.SetRTPCValue(roomSizeParameter, currentRoomSize, targetEmitter);
        }
    }

    void OnValidate()
    {
        if (rayDirections == null || rayDirections.Length != raysCount)
        {
            GenerateFibonacciSphere();
        }
    }
}
