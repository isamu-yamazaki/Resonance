using UnityEngine;

/// <summary>
/// Wwise Smart Occlusion - Volumetric Occlusion with Diffraction
/// Uses cone-based raycasting to simulate realistic sound obstruction and diffraction.
/// </summary>
[RequireComponent(typeof(AkGameObj))]
public class WwiseSmartOcclusion : MonoBehaviour
{
    [Header("Wwise Parameters")]
    [Tooltip("RTPC name for occlusion (0 = clear, 1 = blocked)")]
    public string occlusionParameter = "Occlusion";
    
    [Tooltip("RTPC name for diffraction")]
    public string diffractionParameter = "Diffraction";

    [Header("Volumetric Cone")]
    [Tooltip("Number of rays in cone")]
    [Range(3, 12)]
    public int coneRayCount = 6;
    
    [Tooltip("Cone spread angle")]
    [Range(5f, 60f)]
    public float coneAngle = 30f;
    
    [Tooltip("Maximum check distance")]
    public float maxCheckDistance = 100f;
    
    [Tooltip("Occlusion layer mask")]
    public LayerMask occlusionLayer;

    [Header("Diffraction")]
    [Tooltip("Enable diffraction simulation")]
    public bool enableDiffraction = true;
    
    [Tooltip("Near-field threshold (prevents false occlusion)")]
    public float nearFieldThreshold = 1.5f;
    
    [Tooltip("Diffraction curve (distance to amount)")]
    public AnimationCurve diffractionCurve = AnimationCurve.EaseInOut(0f, 1f, 5f, 0f);

    [Header("Optimization")]
    [Tooltip("Scan rate (Hz)")]
    [Range(1f, 30f)]
    public float scanRate = 5f;
    
    [Tooltip("Distance culling")]
    public float cullingDistance = 50f;

    [Header("Debug")]
    public bool drawDebugRays = false;
    public Color clearColor = Color.green;
    public Color occludedColor = Color.red;
    public Color diffractionColor = Color.yellow;

    // Internal
    private Transform listener;
    private Vector3[] coneDirections;
    private float lastScanTime;
    private float currentOcclusion;
    private float currentDiffraction;
    private float targetOcclusion;
    private float targetDiffraction;
    private const float smoothingSpeed = 5f;

    // Public accessors
    public float Occlusion => currentOcclusion;
    public float Diffraction => currentDiffraction;

    void Start()
    {
        if (occlusionLayer == 0)
        {
            int environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer != -1)
            {
                occlusionLayer = 1 << environmentLayer;
            }
            else
            {
                Debug.LogWarning("[WwiseSmartOcclusion] 'Environment' layer not found! Please set occlusion layer manually or create an 'Environment' layer.");
            }
        }

        FindListener();
        GenerateConeDirections();
        lastScanTime = -1f;
    }

    void Update()
    {
        if (!ShouldProcess())
            return;

        // Time-sliced scanning
        if (Time.time - lastScanTime >= 1f / scanRate)
        {
            PerformOcclusionScan();
            lastScanTime = Time.time;
        }

        // Smooth updates
        SmoothParameters();
    }

    bool ShouldProcess()
    {
        if (listener == null)
        {
            FindListener();
            return false;
        }

        // Distance culling
        float distance = Vector3.Distance(transform.position, listener.position);
        return distance <= cullingDistance;
    }

    void FindListener()
    {
        AkAudioListener akListener = FindAnyObjectByType<AkAudioListener>();
        if (akListener != null)
        {
            listener = akListener.transform;
        }
        else
        {
            AudioListener unityListener = FindAnyObjectByType<AudioListener>();
            if (unityListener != null)
            {
                listener = unityListener.transform;
            }
        }
    }

    void GenerateConeDirections()
    {
        coneDirections = new Vector3[coneRayCount];
        coneDirections[0] = Vector3.zero; // Center ray (calculated dynamically)

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

        // Center ray
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

        // Surrounding rays
        for (int i = 1; i < coneRayCount; i++)
        {
            Vector3 localDir = coneDirections[i];
            Quaternion rotation = Quaternion.LookRotation(dirToListener);
            Vector3 worldDir = rotation * localDir;

            RaycastHit hit;
            if (Physics.Raycast(origin, worldDir, out hit, distanceToListener, occlusionLayer))
            {
                // Near-field logic: prevent false occlusion when next to walls
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

        // Calculate occlusion
        float blockRatio = (float)blockedCount / coneRayCount;
        targetOcclusion = Mathf.Clamp01(blockRatio);

        // Calculate diffraction
        if (enableDiffraction && centerIsBlocked && minDiffractionDist < float.MaxValue)
        {
            targetDiffraction = diffractionCurve.Evaluate(minDiffractionDist);
        }
        else
        {
            targetDiffraction = 0f;
        }
    }

    void SmoothParameters()
    {
        currentOcclusion = Mathf.Lerp(currentOcclusion, targetOcclusion, Time.deltaTime * smoothingSpeed);
        currentDiffraction = Mathf.Lerp(currentDiffraction, targetDiffraction, Time.deltaTime * smoothingSpeed);

        UpdateWwiseParameters();
    }

    void UpdateWwiseParameters()
    {
        AkUnitySoundEngine.SetRTPCValue(occlusionParameter, currentOcclusion, gameObject);
        
        if (enableDiffraction)
        {
            AkUnitySoundEngine.SetRTPCValue(diffractionParameter, currentDiffraction, gameObject);
        }
    }

    void OnValidate()
    {
        if (coneDirections == null || coneDirections.Length != coneRayCount)
        {
            GenerateConeDirections();
        }
    }
}
