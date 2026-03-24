using System.Collections;
using UnityEngine;
using Resonance;
using Resonance.PlayerController;
using Resonance.Match;
using Resonance.Assemblies.SharedGameLogic;

public class PreMatchCameraController : MonoBehaviour
{
    [Header("Orbit")]
    public Camera cinematicCamera;
    public float orbitDistance = 10f;
    public float orbitHeight = 6f;
    public float rotationSpeed = 25f;

    [Header("Drone Feel")]
    public float positionalDriftStrength = 0.18f;
    public float positionalDriftSpeed = 0.6f;
    public float aimLagStrength = 0.06f;
    public float aimLagSpeed = 2.8f;
    public float altitudeBobStrength = 0.35f;
    public float altitudeBobSpeed = 0.45f;
    public float distancePulseStrength = 0.4f;
    public float distancePulseSpeed = 0.3f;
    public float fovBreathStrength = 1.2f;
    public float fovBreathSpeed = 0.25f;
    public float baseFov = 72f;

    private Transform target;
    private Camera playerCamera;
    private bool countdownOrMatchStarted = false;

    private float orbitAngle = 0f;
    private Vector3 driftOffset = Vector3.zero;
    private Vector3 driftVelocity = Vector3.zero;
    private Quaternion smoothLookRotation;

    private float driftNoiseOffsetX;
    private float driftNoiseOffsetZ;
    private float altitudeNoiseOffset;
    private float distanceNoiseOffset;
    private float fovNoiseOffset;

    #region Lifecycle

    private void OnEnable()
    {
        if (ArenaRoundManagerBridge.Instance != null)
        {
            ArenaRoundManagerBridge.Instance.OnMatchCountdownStart += HandleCountdownStart;
            ArenaRoundManagerBridge.Instance.OnMatchTimerElapsed += HandleMatchTimerElapsed;
        }
    }

    private void OnDisable()
    {
        if (ArenaRoundManagerBridge.Instance != null)
        {
            ArenaRoundManagerBridge.Instance.OnMatchCountdownStart -= HandleCountdownStart;
            ArenaRoundManagerBridge.Instance.OnMatchTimerElapsed -= HandleMatchTimerElapsed;
        }
    }

<<<<<<< Updated upstream
    private void HandleCountdownStart(float seconds = 0)
    {
        countdownOrMatchStarted = true;
    }

    private void HandleMatchTimerElapsed(double timeRemaining)
    {
        countdownOrMatchStarted = true;
    }

=======
>>>>>>> Stashed changes
    private IEnumerator Start()
    {
        while (PlayerController.LocalPlayer == null)
            yield return null;

        target = PlayerController.LocalPlayer.transform;
        playerCamera = PlayerController.LocalPlayer.GetComponentInChildren<Camera>(true);

        driftNoiseOffsetX = Random.Range(0f, 100f);
        driftNoiseOffsetZ = Random.Range(0f, 100f);
        altitudeNoiseOffset = Random.Range(0f, 100f);
        distanceNoiseOffset = Random.Range(0f, 100f);
        fovNoiseOffset = Random.Range(0f, 100f);

        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);

        PositionCamera();
        smoothLookRotation = Quaternion.LookRotation(target.position - cinematicCamera.transform.position);

        cinematicCamera.gameObject.SetActive(true);
        cinematicCamera.depth = 100;
        cinematicCamera.fieldOfView = baseFov;

<<<<<<< Updated upstream
        // Orbit while waiting for countdown to start
        while (!countdownOrMatchStarted)
=======
        while (!countdownStarted)
>>>>>>> Stashed changes
        {
            UpdateDroneCamera();
            yield return null;
        }

        EndSequence();
    }

    #endregion

    #region Camera Update

    private void UpdateDroneCamera()
    {
        float time = Time.time;

        orbitAngle += rotationSpeed * Time.deltaTime;

        float altitudeBob = (Mathf.PerlinNoise(time * altitudeBobSpeed, altitudeNoiseOffset) - 0.5f) * 2f * altitudeBobStrength;
        float distancePulse = (Mathf.PerlinNoise(time * distancePulseSpeed, distanceNoiseOffset) - 0.5f) * 2f * distancePulseStrength;

        float currentDistance = orbitDistance + distancePulse;
        float currentHeight = orbitHeight + altitudeBob;

        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector3 orbitPos = target.position
            + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * currentDistance
            + Vector3.up * currentHeight;

        float driftX = (Mathf.PerlinNoise(time * positionalDriftSpeed, driftNoiseOffsetX) - 0.5f) * 2f;
        float driftZ = (Mathf.PerlinNoise(time * positionalDriftSpeed, driftNoiseOffsetZ) - 0.5f) * 2f;
        Vector3 targetDrift = new Vector3(driftX, 0f, driftZ) * positionalDriftStrength;
        driftOffset = Vector3.SmoothDamp(driftOffset, targetDrift, ref driftVelocity, 0.4f);

        cinematicCamera.transform.position = orbitPos + driftOffset;

        float aimOffsetX = (Mathf.PerlinNoise(time * aimLagSpeed, driftNoiseOffsetX + 50f) - 0.5f) * 2f;
        float aimOffsetY = (Mathf.PerlinNoise(time * aimLagSpeed, driftNoiseOffsetZ + 50f) - 0.5f) * 2f;
        Vector3 aimTarget = target.position
            + target.up * 1.2f
            + new Vector3(aimOffsetX, aimOffsetY, 0f) * aimLagStrength;

        Quaternion desiredLook = Quaternion.LookRotation(aimTarget - cinematicCamera.transform.position);
        smoothLookRotation = Quaternion.Slerp(smoothLookRotation, desiredLook, Time.deltaTime * 4.5f);
        cinematicCamera.transform.rotation = smoothLookRotation;

        float fovBreath = (Mathf.PerlinNoise(time * fovBreathSpeed, fovNoiseOffset) - 0.5f) * 2f * fovBreathStrength;
        cinematicCamera.fieldOfView = baseFov + fovBreath;
    }

    #endregion

    #region Helpers

    private void PositionCamera()
    {
        Vector3 startPos =
            target.position
            - target.forward * orbitDistance
            + Vector3.up * orbitHeight;

        cinematicCamera.transform.position = startPos;
        cinematicCamera.transform.LookAt(target);
    }

    private void EndSequence()
    {
        GetComponent<CinematicCameraPostProcessing>()?.OnCinematicEnd();

        if (playerCamera != null)
            playerCamera.gameObject.SetActive(true);

        cinematicCamera.gameObject.SetActive(false);
    }

    private void HandleCountdownStart(float seconds)
    {
        countdownStarted = true;
    }

    #endregion
}
