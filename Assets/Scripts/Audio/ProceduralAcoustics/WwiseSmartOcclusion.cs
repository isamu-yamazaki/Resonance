using UnityEngine;

/// <summary>
/// Wwise Smart Occlusion - Volumetric Occlusion with Diffraction
/// Uses cone-based raycasting to simulate realistic sound obstruction and diffraction.
/// </summary>
[RequireComponent(typeof(AkGameObj))]
public class WwiseSmartOcclusion : MonoBehaviour
{
    [Header("Wwise Parameters")]
    public string occlusionParameter = "Occlusion";
    public string diffractionParameter = "Diffraction";

    [Header("Volumetric Cone")]
    [Range(3, 12)]
    public int coneRayCount = 6;
    [Range(5f, 60f)]
    public float coneAngle = 30f;
    public float maxCheckDistance = 100f;
    public LayerMask occlusionLayer;

    [Header("Diffraction")]
    public bool enableDiffraction = true;
    public float nearFieldThreshold = 1.5f;
    public AnimationCurve diffractionCurve = AnimationCurve.EaseInOut(0f, 1f, 5f, 0f);

    [Header("Optimization")]
    [Range(1f, 30f)]
    public float scanRate = 5f;
    public float cullingDistance = 50f;

    [Header("Debug")]
    public bool drawDebugRays = false;
    public Color clearColor = Color.green;
    public Color occludedColor = Color.red;
    public Color diffractionColor = Color.yellow;

    private Transform listener;
    private Vector3[] coneDirections;
    private float lastScanTime;
    private float currentOcclusion;
    private float currentDiffraction;
    private float targetOcclusion;
    private float targetDiffraction;
    private float lastSentOcclusion = -1f;
    private float lastSentDiffraction = -1f;
    private float listenerSearchCooldown;
    private const float listenerSearchInterval = 1f;
    private const float smoothingSpeed = 5f;
    private const float rtpcSendThreshold = 0.005f;

    public float Occlusion => currentOcclusion;
    public float Diffraction => currentDiffraction;

    void Start()
    {
        if (occlusionLayer == 0)
        {
            int environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer != -1)
                occlusionLayer = 1 << environmentLayer;
            else
                Debug.LogWarning("[WwiseSmartOcclusion] 'Environment' layer not found! Please set occlusion layer manually.");
        }

        FindListener();
        GenerateConeDirections();
        lastScanTime = -1f;
    }

    void Update()
    {
        if (!ShouldProcess())
            return;

        if (Time.time - lastScanTime >= 1f / scanRate)
        {
            PerformOcclusionScan();
            lastScanTime = Time.time;
        }

        SmoothParameters();
    }

    bool ShouldProcess()
    {
        if (listener == null)
        {
            listenerSearchCooldown -= Time.deltaTime;
            if (listenerSearchCooldown <= 0f)
            {
                FindListener();
                listenerSearchCooldown = listenerSearchInterval;
            }
            return false;
        }

        return Vector3.Distance(transform.position, listener.position) <= cullingDistance;
    }

    void FindListener()
    {
        AkAudioListener akListener = FindAnyObjectByType<AkAudioListener>();
        if (akListener != null)
        {
            listener = akListener.transform;
            return;
        }

        AudioListener unityListener = FindAnyObjectByType<AudioListener>();
        if (unityListener != null)
            listener = unityListener.transform;
    }

    void GenerateConeDirections()
    {
        coneDirections = new Vector3[coneRayCount];
        coneDirections[0] = Vector3.zero;

        if (coneRayCount > 1)
        {
            float angleStep = 360f / (coneRayCount - 1);
            for (int i = 1; i < coneRayCount; i++)
            {
                float angle = angleStep * (i - 1) * Mathf.Deg2Rad;
                float coneRad = coneAngle * Mathf.Deg2Rad;

                coneDirections[i] = new Vector3(
                    Mathf.Cos(angle) * Mathf.Sin(coneRad),
                    Mathf.Sin(angle) * Mathf.Sin(coneRad),
                    Mathf.Cos(coneRad)
                );
            }
        }
    }

    void PerformOcclusionScan()
    {
        if (listener == null)
            return;

        Vector3 origin = transform.position;
        Vector3 toListener = listener.position - origin;
        float distanceToListener = toListener.magnitude;
        Vector3 dirToListener = toListener / distanceToListener;

        int blockedCount = 0;
        bool centerIsBlocked = false;
        float minDiffractionDist = float.MaxValue;

        RaycastHit centerHit;
        if (Physics.Raycast(origin, dirToListener, out centerHit, distanceToListener, occlusionLayer))
        {
            centerIsBlocked = true;
            blockedCount++;

            if (drawDebugRays)
                Debug.DrawLine(origin, centerHit.point, occludedColor, 1f / scanRate);
        }
        else if (drawDebugRays)
        {
            Debug.DrawLine(origin, listener.position, clearColor, 1f / scanRate);
        }

        for (int i = 1; i < coneRayCount; i++)
        {
            Vector3 localDir = coneDirections[i];
            Quaternion rotation = Quaternion.LookRotation(dirToListener);
            Vector3 worldDir = rotation * localDir;

            RaycastHit hit;
            if (Physics.Raycast(origin, worldDir, out hit, distanceToListener, occlusionLayer))
            {
                float distFromHitToListener = distanceToListener - hit.distance;
                bool isNearField = (distFromHitToListener < nearFieldThreshold) && (!centerIsBlocked);

                if (!isNearField)
                {
                    blockedCount++;
                    minDiffractionDist = Mathf.Min(minDiffractionDist, distFromHitToListener);

                    if (drawDebugRays)
                        Debug.DrawLine(origin, hit.point, occludedColor, 1f / scanRate);
                }
                else if (drawDebugRays)
                {
                    Debug.DrawLine(origin, hit.point, diffractionColor, 1f / scanRate);
                }
            }
            else if (drawDebugRays)
            {
                Debug.DrawRay(origin, worldDir * distanceToListener, clearColor, 1f / scanRate);
            }
        }

        targetOcclusion = Mathf.Clamp01((float)blockedCount / coneRayCount);
        targetDiffraction = (enableDiffraction && centerIsBlocked && minDiffractionDist < float.MaxValue)
            ? diffractionCurve.Evaluate(minDiffractionDist)
            : 0f;
    }

    void SmoothParameters()
    {
        currentOcclusion = Mathf.Lerp(currentOcclusion, targetOcclusion, Time.deltaTime * smoothingSpeed);
        currentDiffraction = Mathf.Lerp(currentDiffraction, targetDiffraction, Time.deltaTime * smoothingSpeed);

        UpdateWwiseParameters();
    }

    void UpdateWwiseParameters()
    {
        if (Mathf.Abs(currentOcclusion - lastSentOcclusion) >= rtpcSendThreshold)
        {
            AkUnitySoundEngine.SetRTPCValue(occlusionParameter, currentOcclusion, gameObject);
            lastSentOcclusion = currentOcclusion;
        }

        if (enableDiffraction && Mathf.Abs(currentDiffraction - lastSentDiffraction) >= rtpcSendThreshold)
        {
            AkUnitySoundEngine.SetRTPCValue(diffractionParameter, currentDiffraction, gameObject);
            lastSentDiffraction = currentDiffraction;
        }
    }

    void OnValidate()
    {
        if (coneDirections == null || coneDirections.Length != coneRayCount)
            GenerateConeDirections();
    }
}
