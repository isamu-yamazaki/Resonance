using System.Collections;
using UnityEngine;
using Resonance;
using Resonance.PlayerController;
using Resonance.Match;

public class PreMatchCameraController : MonoBehaviour
{
    [Header("Cameras")]
    public Camera cinematicCamera;

    [Header("Drone Object")]
    public Drone dronePrefab;

    [Header("Path")]
    public float flightSpeed = 2.5f;
    public float lookSmoothing = 4.5f;

    [Header("Hero Position")]
    public Vector3 heroOffset = new Vector3(0f, 1.4f, 5f);
    public float heroApproachSpeed = 3.5f;
    public float heroHoverDuration = 0.6f;

    [Header("Drone Feel")]
    public float positionalDriftStrength = 0.18f;
    public float positionalDriftSpeed = 0.6f;
    public float aimLagStrength = 0.06f;
    public float aimLagSpeed = 2.8f;
    public float altitudeBobStrength = 0.35f;
    public float altitudeBobSpeed = 0.45f;
    public float fovBreathStrength = 1.2f;
    public float fovBreathSpeed = 0.25f;
    public float baseFov = 72f;

    [Header("Waypoint Offsets (from spawn)")]
    public Vector3[] waypointOffsets;

    private static readonly Vector3[] DefaultWaypointOffsets = new Vector3[]
    {
        new Vector3( -8f,  6f, -10f),
        new Vector3(  4f,  9f,  -6f),
        new Vector3( 10f,  5f,   4f),
        new Vector3(  2f,  8f,  10f),
        new Vector3( -6f,  6f,   6f),
        new Vector3(-10f,  4f,  -2f),
    };

    private Transform target;
    private Camera playerCamera;
    private bool countdownStarted = false;

    private Vector3 driftOffset = Vector3.zero;
    private Vector3 driftVelocity = Vector3.zero;
    private Quaternion smoothLookRotation;

    private float driftNoiseOffsetX = 17.3f;
    private float driftNoiseOffsetZ = 43.7f;
    private float altitudeNoiseOffset = 61.2f;
    private float fovNoiseOffset = 82.5f;

    private void OnEnable()
    {
        if (ArenaRoundManagerBridge.Instance != null)
            ArenaRoundManagerBridge.Instance.OnMatchCountdownStart += HandleCountdownStart;
    }

    private void OnDisable()
    {
        if (ArenaRoundManagerBridge.Instance != null)
            ArenaRoundManagerBridge.Instance.OnMatchCountdownStart -= HandleCountdownStart;
    }

    private void HandleCountdownStart(float seconds)
    {
        countdownStarted = true;
    }

    private IEnumerator Start()
    {
        while (PlayerController.LocalPlayer == null)
            yield return null;

        target = PlayerController.LocalPlayer.transform;
        playerCamera = PlayerController.LocalPlayer.GetComponentInChildren<Camera>(true);

        if (waypointOffsets == null || waypointOffsets.Length < 2)
            waypointOffsets = DefaultWaypointOffsets;

        cinematicCamera.gameObject.SetActive(true);
        cinematicCamera.depth = 100;
        cinematicCamera.fieldOfView = baseFov;

        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);

        cinematicCamera.transform.position = target.position + waypointOffsets[0];
        cinematicCamera.transform.LookAt(target.position + Vector3.up * 1.2f);
        smoothLookRotation = cinematicCamera.transform.rotation;

        Drone droneObject = dronePrefab != null ? Instantiate(dronePrefab) : null;
        if (droneObject != null)
            droneObject.SetFollowTarget(cinematicCamera.transform);

        yield return FlyPath();

        yield return FlyToHeroPosition();

        EndSequence();

        if (droneObject != null)
            droneObject.FlyAway(Vector3.up);
    }

    private IEnumerator FlyPath()
    {
        int waypointCount = waypointOffsets.Length;
        int currentWaypoint = 0;

        while (!countdownStarted)
        {
            int nextWaypoint = (currentWaypoint + 1) % waypointCount;

            Vector3 from = target.position + waypointOffsets[currentWaypoint];
            Vector3 to = target.position + waypointOffsets[nextWaypoint];
            Vector3 prev = target.position + waypointOffsets[(currentWaypoint - 1 + waypointCount) % waypointCount];
            Vector3 after = target.position + waypointOffsets[(nextWaypoint + 1) % waypointCount];

            float segmentLength = Vector3.Distance(from, to);
            float duration = segmentLength / flightSpeed;
            float elapsed = 0f;

            while (elapsed < duration && !countdownStarted)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                cinematicCamera.transform.position = CatmullRom(prev, from, to, after, t);
                ApplyDroneFeel();

                yield return null;
            }

            currentWaypoint = nextWaypoint;
        }
    }

    private IEnumerator FlyToHeroPosition()
    {
        // Hero position is in front of and slightly above the player, facing them
        Vector3 heroWorldPos = target.position + target.forward * heroOffset.z + Vector3.up * heroOffset.y;

        while (Vector3.Distance(cinematicCamera.transform.position, heroWorldPos) > 0.1f)
        {
            cinematicCamera.transform.position = Vector3.MoveTowards(
                cinematicCamera.transform.position,
                heroWorldPos,
                heroApproachSpeed * Time.deltaTime
            );

            // Look back at the player during approach
            Quaternion desiredLook = Quaternion.LookRotation(target.position + Vector3.up * 1.2f - cinematicCamera.transform.position);
            smoothLookRotation = Quaternion.Slerp(smoothLookRotation, desiredLook, Time.deltaTime * lookSmoothing);
            cinematicCamera.transform.rotation = smoothLookRotation;

            ApplyDroneFeel();
            yield return null;
        }

        // Hover briefly so the player sees the drone facing them
        float elapsed = 0f;
        while (elapsed < heroHoverDuration)
        {
            elapsed += Time.deltaTime;
            ApplyDroneFeel();
            yield return null;
        }
    }

    private void ApplyDroneFeel()
    {
        float time = Time.time;

        float altitudeBob = (Mathf.PerlinNoise(time * altitudeBobSpeed, altitudeNoiseOffset) - 0.5f) * 2f * altitudeBobStrength;
        cinematicCamera.transform.position += Vector3.up * altitudeBob * Time.deltaTime;

        float driftX = (Mathf.PerlinNoise(time * positionalDriftSpeed, driftNoiseOffsetX) - 0.5f) * 2f;
        float driftZ = (Mathf.PerlinNoise(time * positionalDriftSpeed, driftNoiseOffsetZ) - 0.5f) * 2f;
        Vector3 targetDrift = new Vector3(driftX, 0f, driftZ) * positionalDriftStrength;
        driftOffset = Vector3.SmoothDamp(driftOffset, targetDrift, ref driftVelocity, 0.4f);
        cinematicCamera.transform.position += driftOffset * Time.deltaTime;

        float aimOffsetX = (Mathf.PerlinNoise(time * aimLagSpeed, driftNoiseOffsetX + 50f) - 0.5f) * 2f;
        float aimOffsetY = (Mathf.PerlinNoise(time * aimLagSpeed, driftNoiseOffsetZ + 50f) - 0.5f) * 2f;
        Vector3 aimTarget = target.position
            + target.up * 1.2f
            + new Vector3(aimOffsetX, aimOffsetY, 0f) * aimLagStrength;

        Quaternion desiredLook = Quaternion.LookRotation(aimTarget - cinematicCamera.transform.position);
        smoothLookRotation = Quaternion.Slerp(smoothLookRotation, desiredLook, Time.deltaTime * lookSmoothing);
        cinematicCamera.transform.rotation = smoothLookRotation;

        float fovBreath = (Mathf.PerlinNoise(time * fovBreathSpeed, fovNoiseOffset) - 0.5f) * 2f * fovBreathStrength;
        cinematicCamera.fieldOfView = baseFov + fovBreath;
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1
            + (-p0 + p2) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private void EndSequence()
    {
        GetComponent<CinematicCameraPostProcessing>()?.OnCinematicEnd();

        if (playerCamera != null)
            playerCamera.gameObject.SetActive(true);

        cinematicCamera.gameObject.SetActive(false);
    }
}
