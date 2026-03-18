using Resonance.PlayerController;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1)]
public class Zipline : MonoBehaviour, IInteractable
{
    public Collider InteractRange { get; set; }

    [Header("References")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Settings")]
    [SerializeField] private float ziplineSpeed = 10f;
    [SerializeField] private float handReachOffset = 1.0f;

    [Header("Line Renderer")]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material lineMaterial;

    private LineRenderer lineRenderer;

    private GameObject currentPlayer;
    private CharacterController playerController;
    private PlayerLocomotionInput playerLocomotionInput;
    private PlayerState playerState;
    private Transform playerCameraTransform;

    private Vector3 pointAWorld;
    private Vector3 pointBWorld;
    private float playerHeight;

    private Vector3 currentCablePosition;
    private Vector3 targetCablePosition;

    private bool isRiding;
    private bool jumpLatch;

    #region Unity Messages

    private void Start()
    {
        InteractRange = GetComponent<Collider>();
        SetupLineRenderer();
        RecalculateEndpoints();
        RefreshLineRenderer();
    }

    private void Update()
    {
        TrackEndpointChanges();

        if (!isRiding)
            return;

        if (currentPlayer == null)
        {
            ForceCleanup();
            return;
        }

        if (playerLocomotionInput.JumpPressed || Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpLatch = true;

        if (jumpLatch)
        {
            Dismount();
            return;
        }

        HandleMovement();
    }

    private void OnDrawGizmos()
    {
        if (pointA == null || pointB == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1f);
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pointA.position, pointB.position);
        Gizmos.DrawWireSphere(pointA.position, 0.5f);
        Gizmos.DrawWireSphere(pointB.position, 0.5f);
    }

    #endregion

    #region IInteractable

    public void Interact(GameObject interactor)
    {
        if (isRiding)
            return;

        PlayerState state = interactor.GetComponent<PlayerState>();
        if (state == null || state.IsDead() || state.IsInShop())
            return;

        PlayerLocomotionInput locomotionInput = interactor.GetComponent<PlayerLocomotionInput>();
        if (locomotionInput == null)
        {
            Debug.LogError("Zipline: interactor is missing PlayerLocomotionInput.");
            return;
        }

        CharacterController characterController = interactor.GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("Zipline: interactor is missing CharacterController.");
            return;
        }

        playerState = state;
        playerLocomotionInput = locomotionInput;
        playerController = characterController;
        currentPlayer = interactor;
        playerHeight = playerController.height;

        // Get the camera transform — try the CinemachineCamera child first,
        // then any Camera in children, then fall back to Camera.main
        PlayerController pc = interactor.GetComponent<PlayerController>();
        if (pc != null)
        {
            // CinemachineCamera is the virtual camera; the brain (real Camera) is on Camera.main
            // but we can use the virtual camera transform for direction since it matches look direction
            Unity.Cinemachine.CinemachineCamera vcam = interactor.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
            playerCameraTransform = vcam != null ? vcam.transform : null;
        }

        if (playerCameraTransform == null)
        {
            Camera cam = interactor.GetComponentInChildren<Camera>();
            playerCameraTransform = cam != null ? cam.transform : Camera.main?.transform;
        }

        Debug.Log($"[Zipline] playerCameraTransform: {playerCameraTransform}, forward: {playerCameraTransform?.forward}");

        // Snap to nearest point on cable
        Vector3 playerPos = interactor.transform.position;
        Vector3 cableDir = (pointBWorld - pointAWorld).normalized;
        float cableLength = Vector3.Distance(pointAWorld, pointBWorld);
        float projection = Vector3.Dot(playerPos - pointAWorld, cableDir);
        float t = Mathf.Clamp01(projection / cableLength);
        currentCablePosition = Vector3.Lerp(pointAWorld, pointBWorld, t);

        // Ride toward whichever endpoint the player's camera is facing
        if (playerCameraTransform != null)
        {
            float dotToA = Vector3.Dot(playerCameraTransform.forward, (pointAWorld - currentCablePosition).normalized);
            float dotToB = Vector3.Dot(playerCameraTransform.forward, (pointBWorld - currentCablePosition).normalized);
            targetCablePosition = dotToB > dotToA ? pointBWorld : pointAWorld;
            Debug.Log($"[Zipline] dotToA={dotToA:F3} dotToB={dotToB:F3} target={targetCablePosition}");
        }
        else
        {
            float distToA = Vector3.Distance(playerPos, pointAWorld);
            float distToB = Vector3.Distance(playerPos, pointBWorld);
            targetCablePosition = distToA > distToB ? pointAWorld : pointBWorld;
            Debug.LogWarning("[Zipline] No camera found, falling back to distance-based direction.");
        }

        playerState.SetPlayerMovementState(PlayerMovementState.Ziplining);

        isRiding = true;
        jumpLatch = false;
    }

    #endregion

    #region Zipline Logic

    private void HandleMovement()
    {
        currentCablePosition = Vector3.MoveTowards(
            currentCablePosition,
            targetCablePosition,
            ziplineSpeed * Time.deltaTime
        );

        float hangOffset = -(playerHeight * 0.5f + handReachOffset);
        Vector3 targetPosition = currentCablePosition + Vector3.up * hangOffset;
        Vector3 delta = targetPosition - currentPlayer.transform.position;
        playerController.Move(delta);

        Vector3 travelDirection = targetCablePosition - currentCablePosition;
        if (travelDirection.sqrMagnitude > 0.001f)
        {
            Vector3 flatDirection = new Vector3(travelDirection.x, 0f, travelDirection.z);
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                currentPlayer.transform.rotation = Quaternion.Lerp(
                    currentPlayer.transform.rotation,
                    Quaternion.LookRotation(flatDirection),
                    Time.deltaTime * 10f
                );
            }
        }

        if (Vector3.Distance(currentCablePosition, targetCablePosition) < 0.05f)
            Dismount();
    }

    private void Dismount()
    {
        if (currentPlayer != null)
            playerState.SetPlayerMovementState(PlayerMovementState.Falling);

        ForceCleanup();
    }

    private void ForceCleanup()
    {
        currentPlayer = null;
        playerController = null;
        playerLocomotionInput = null;
        playerState = null;
        playerCameraTransform = null;
        isRiding = false;
        jumpLatch = false;
    }

    #endregion

    #region Line Renderer

    private void TrackEndpointChanges()
    {
        if (pointA == null || pointB == null)
            return;

        if (!pointA.hasChanged && !pointB.hasChanged)
            return;

        RecalculateEndpoints();
        RefreshLineRenderer();
        pointA.hasChanged = false;
        pointB.hasChanged = false;
    }

    private void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        lineRenderer.material = lineMaterial != null
            ? lineMaterial
            : new Material(Shader.Find("Sprites/Default")) { color = Color.gray };
    }

    private void RecalculateEndpoints()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogError("Zipline: point A or point B not assigned.");
            pointAWorld = transform.position;
            pointBWorld = transform.position + Vector3.forward * 10f;
        }
        else
        {
            pointAWorld = pointA.position;
            pointBWorld = pointB.position;
        }
    }

    private void RefreshLineRenderer()
    {
        if (lineRenderer == null || pointA == null || pointB == null)
            return;

        lineRenderer.SetPosition(0, pointA.position);
        lineRenderer.SetPosition(1, pointB.position);
    }

    #endregion
}
