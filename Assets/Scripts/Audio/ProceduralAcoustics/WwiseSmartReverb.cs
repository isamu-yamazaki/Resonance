using UnityEngine;

/// <summary>
/// Wwise Smart Reverb - Volumetric Raycasting for Room Acoustics
/// Uses Fibonacci Sphere Sampling to calculate room parameters procedurally.
/// No manual reverb zones required!
/// </summary>
public class WwiseSmartReverb : MonoBehaviour
{
    [Header("Mode")]
    public bool isGlobal = true;
    public GameObject targetEmitter;

    [Header("Wwise Parameters")]
    public string enclosureParameter = "Enclosure";
    public string roomSizeParameter = "RoomSize";

    [Header("Raycast Settings")]
    [Range(10, 60)]
    public int raysCount = 30;
    public float maxDistance = 50f;
    public LayerMask environmentLayer;

    [Header("Update Rate")]
    [Range(1f, 30f)]
    public float scanRate = 5f;

    [Header("Calibration")]
    public AnimationCurve enclosureCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve roomSizeCurve = AnimationCurve.Linear(0f, 5f, 50f, 100f);

    [Header("Debug")]
    public bool drawRays = false;
    public Color hitColor = Color.red;
    public Color missColor = Color.green;

    private Vector3[] rayDirections;
    private float lastScanTime;
    private float currentEnclosure;
    private float currentRoomSize;
    private float targetEnclosure;
    private float targetRoomSize;
    private float lastSentEnclosure = -1f;
    private float lastSentRoomSize = -1f;
    private const float smoothingSpeed = 5f;
    private const float rtpcSendThreshold = 0.005f;

    public float EnclosureFactor => currentEnclosure;
    public float RoomSize => currentRoomSize;

    void Start()
    {
        GenerateFibonacciSphere();
        lastScanTime = -1f;
    }

    void Update()
    {
        if (Time.time - lastScanTime >= 1f / scanRate)
        {
            PerformScan();
            lastScanTime = Time.time;
        }

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
        if (Mathf.Abs(currentEnclosure - lastSentEnclosure) >= rtpcSendThreshold)
        {
            if (isGlobal)
                AkUnitySoundEngine.SetRTPCValue(enclosureParameter, currentEnclosure);
            else if (targetEmitter != null)
                AkUnitySoundEngine.SetRTPCValue(enclosureParameter, currentEnclosure, targetEmitter);

            lastSentEnclosure = currentEnclosure;
        }

        if (Mathf.Abs(currentRoomSize - lastSentRoomSize) >= rtpcSendThreshold)
        {
            if (isGlobal)
                AkUnitySoundEngine.SetRTPCValue(roomSizeParameter, currentRoomSize);
            else if (targetEmitter != null)
                AkUnitySoundEngine.SetRTPCValue(roomSizeParameter, currentRoomSize, targetEmitter);

            lastSentRoomSize = currentRoomSize;
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
