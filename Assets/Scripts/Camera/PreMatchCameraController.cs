using System.Collections;
using UnityEngine;
using Resonance;
using Resonance.PlayerController;
using Resonance.Match;

public class PreMatchCameraController : MonoBehaviour
{
    public Camera cinematicCamera;
    public float orbitDistance = 10f;
    public float orbitHeight = 6f;
    public float rotationSpeed = 25f;

    private Transform target;
    private Camera playerCamera;
    private bool countdownStarted = false;

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
        // Wait for the local player
        while (PlayerController.LocalPlayer == null)
            yield return null;

        target = PlayerController.LocalPlayer.transform;

        // Find the player camera automatically
        playerCamera = PlayerController.LocalPlayer.GetComponentInChildren<Camera>(true);

        // Activate cinematic camera first
        cinematicCamera.gameObject.SetActive(true);
        cinematicCamera.depth = 100;

        // Disable player camera
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);

        // Position camera behind/above the player
        PositionCamera();

        // Orbit while waiting for countdown to start
        while (!countdownStarted)
        {
            cinematicCamera.transform.RotateAround(target.position, Vector3.up, rotationSpeed * Time.deltaTime);
            cinematicCamera.transform.LookAt(target);
            yield return null;
        }

        // Switch to player camera when countdown begins
        EndSequence();
    }

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
}
