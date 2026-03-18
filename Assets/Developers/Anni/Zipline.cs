using Resonance.PlayerController;
using UnityEngine;

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

    private Vector3 pointAWorld;
    private Vector3 pointBWorld;
    private float playerHeight;

    // World-space position the player is currently at on the cable
    private Vector3 currentCablePosition;

    // World-space destination the player is riding toward
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

        if (playerLocomotionInput.JumpPressed)
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

        // Snap to nearest point on the cable
        Vector3 playerPos = interactor.transform.position;
        Vector3 cableDir = (pointBWorld - pointAWorld).normalized;
        float projection = Vector3.Dot(playerPos - pointAWorld, cableDir);
        float cableLength = Vector3.Distance(pointAWorld, pointBWorld);
        float t = Mathf.Clamp01(projection / cableLength);
        currentCablePosition = Vector3.Lerp(pointAWorld, pointBWorld, t);

        // Ride toward whichever end is farther away (i.e. the one we're not near)
        float distToA = Vector3.Distance(playerPos, pointAWorld);
        float distToB = Vector3.Distance(playerPos, pointBWorld);
        targetCablePosition = distToA > distToB ? pointAWorld : pointBWorld;

        playerState.SetPlayerMovementState(PlayerMovementState.Ziplining);

        isRiding = true;
        jumpLatch = false;
    }

    #endregion

    #region Zipline Logic

    private void HandleMovement()
    {
        // Step toward target along cable
        currentCablePosition = Vector3.MoveTowards(
            currentCablePosition,
            targetCablePosition,
            ziplineSpeed * Time.deltaTime
        );

        // Position player hanging below cable position
        float hangOffset = -(playerHeight * 0.5f + handReachOffset);
        Vector3 targetPosition = currentCablePosition + Vector3.up * hangOffset;
        Vector3 delta = targetPosition - currentPlayer.transform.position;
        playerController.Move(delta);

        // Face the direction of travel
        Vector3 travelDirection = (targetCablePosition - currentCablePosition);
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

        // Dismount on arrival
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
