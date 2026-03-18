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
    [SerializeField] private ZiplineMode ziplineMode = ZiplineMode.Horizontal;
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
    private Vector3 cableDirection;
    private float cableLength;
    private float playerHeight;

    // Horizontal mode
    private float cableProgress;

    // Vertical mode
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

        if (ziplineMode == ZiplineMode.Horizontal)
            HandleHorizontalMovement();
        else
            HandleVerticalMovement();
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

        Unity.Cinemachine.CinemachineCamera vcam = interactor.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
        playerCameraTransform = vcam != null ? vcam.transform : null;

        if (playerCameraTransform == null)
        {
            Camera cam = interactor.GetComponentInChildren<Camera>();
            playerCameraTransform = cam != null ? cam.transform : Camera.main?.transform;
        }

        Vector3 playerPos = interactor.transform.position;
        float projection = Vector3.Dot(playerPos - pointAWorld, cableDirection);
        float t = Mathf.Clamp01(projection / cableLength);

        if (ziplineMode == ZiplineMode.Horizontal)
        {
            cableProgress = t;
        }
        else
        {
            currentCablePosition = Vector3.Lerp(pointAWorld, pointBWorld, t);

            if (playerCameraTransform != null)
            {
                float dotToA = Vector3.Dot(playerCameraTransform.forward, (pointAWorld - currentCablePosition).normalized);
                float dotToB = Vector3.Dot(playerCameraTransform.forward, (pointBWorld - currentCablePosition).normalized);
                targetCablePosition = dotToB > dotToA ? pointBWorld : pointAWorld;
            }
            else
            {
                float distToA = Vector3.Distance(playerPos, pointAWorld);
                float distToB = Vector3.Distance(playerPos, pointBWorld);
                targetCablePosition = distToA > distToB ? pointAWorld : pointBWorld;
            }
        }

        playerState.SetPlayerMovementState(PlayerMovementState.Ziplining);

        isRiding = true;
        jumpLatch = false;
    }

    #endregion

    #region Zipline Logic

    private void HandleHorizontalMovement()
    {
        float moveInput = GetMoveInput();

        cableProgress += moveInput * ziplineSpeed / cableLength * Time.deltaTime;
        cableProgress = Mathf.Clamp01(cableProgress);

        Vector3 cablePosition = Vector3.Lerp(pointAWorld, pointBWorld, cableProgress);
        float hangOffset = -(playerHeight + handReachOffset);
        Vector3 targetPosition = cablePosition + Vector3.up * hangOffset;
        Vector3 delta = targetPosition - currentPlayer.transform.position;
        playerController.Move(delta);

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            Vector3 faceDirection = cableDirection * Mathf.Sign(moveInput);
            Vector3 flatDirection = new Vector3(faceDirection.x, 0f, faceDirection.z);
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                currentPlayer.transform.rotation = Quaternion.Lerp(
                    currentPlayer.transform.rotation,
                    Quaternion.LookRotation(flatDirection),
                    Time.deltaTime * 10f
                );
            }
        }

        if (cableProgress >= 1f || cableProgress <= 0f)
            Dismount();
    }

    private void HandleVerticalMovement()
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

    private float GetMoveInput()
    {
        if (playerCameraTransform == null)
            return 0f;

        Vector2 input = playerLocomotionInput.MovementInput;
        if (input.sqrMagnitude <= 0.01f)
            return 0f;

        Vector3 cameraForward = playerCameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = playerCameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 desiredDirection = (cameraForward * input.y + cameraRight * input.x).normalized;
        return Vector3.Dot(desiredDirection, cableDirection);
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
        cableProgress = 0f;
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

        cableDirection = (pointBWorld - pointAWorld).normalized;
        cableLength = Vector3.Distance(pointAWorld, pointBWorld);
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

public enum ZiplineMode
{
    Horizontal,
    Vertical
}
