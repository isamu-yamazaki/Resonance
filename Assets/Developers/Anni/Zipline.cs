using System.Runtime.CompilerServices;
using Resonance.PlayerController;
using UnityEngine;

public class Zipline : MonoBehaviour, IInteractable
{
    public Collider InteractRange { get; set; }

    [SerializeField] public Transform startPoint;
    [SerializeField] public Transform endPoint;
    [SerializeField] public GameObject headCamera;
    public float ziplineSpeed = 10f;
    private PlayerLocomotionInput _playerLocomotionInput;
    
    public float lineWidth = 0.1f;
    public Material lineMaterial;
    private LineRenderer lineRenderer;
    private GameObject currentPlayer;
    private bool isRiding = false;
    private bool isDismounting = false;
    private float ziplineProgress = 0f;
    private CharacterController playerController;
    private Rigidbody playerRigidbody;
    private Vector3 ziplineDirection;
    private float ziplineLength;
    private Vector3 ziplineStartPoint;
    private Vector3 ziplineEndPoint;
    private float playerHeight;
    public float handReachOffset = 1.0f;
    
    void Start()
    {
        SetupLineRenderer();
        CalculateZiplineEndpoints();
        UpdateLineRenderer();
    }
    
    void Update()
    {
        // Update line in case points moved
        if (startPoint != null && endPoint != null)
        {
            if (startPoint.hasChanged || endPoint.hasChanged)
            {
                CalculateZiplineEndpoints();
                UpdateLineRenderer();
                startPoint.hasChanged = false;
                endPoint.hasChanged = false;
            }
        }

        //Debug.Log("Riding: " + isRiding);
        //Debug.Log("Dismounting: " + isDismounting);

        if (playerController.enabled)
        {
            Debug.Log("player controller enabled");
        }
        /*Debug.Log("player controller enabled: " + playerController.enabled);
        Debug.Log("player kinematic: " + playerRigidbody.isKinematic);
        Debug.Log("current player: " +  currentPlayer);
        Debug.Log("_playerLocomotionInput: " +  _playerLocomotionInput);
        Debug.Log("playerController: " +  playerController);
        Debug.Log("playerRigidbody: " +  playerRigidbody);*/
        
        if (isRiding && !isDismounting)
        {
            HandleZiplineMovement();

            if (_playerLocomotionInput.JumpPressed)
            {
                isDismounting = true;
                Debug.Log("Player Pressed Space");
                DismountZipline();
                Debug.Log("After Dismount");
            }
        }
    }
    
    public void Interact(GameObject interactor)
    {
        //code for going on da zipline
        //use player interactor to do anything needed to player

        if (isRiding || isDismounting)
        {
            Debug.Log("player is already riding zipline");
            return;
        }
        
        Debug.Log("started zipline interaction");
        // Start riding the zipline
        currentPlayer = interactor;
        isRiding = true;
        isDismounting = false;
        
        _playerLocomotionInput = interactor.GetComponent<PlayerLocomotionInput>();
    
        if (_playerLocomotionInput == null)
        {
            Debug.LogError("Player does not have PlayerLocomotionInput component!");
            return;
        }
        
        playerHeight = headCamera.transform.position.y - interactor.transform.position.y;
        
        // Find closest point on zipline to attach player
        Vector3 playerPos = interactor.transform.position;
        Vector3 ziplineVector = ziplineEndPoint - ziplineStartPoint;
        
        // Project player position on zipline
        float projectionLength = Vector3.Dot(playerPos - ziplineStartPoint, ziplineVector.normalized);
        projectionLength = Mathf.Clamp(projectionLength, 0f, ziplineLength);
        
        // Set initial progress based on where player grabbed zipline
        ziplineProgress = projectionLength / ziplineLength;
        
        playerController = interactor.GetComponent<CharacterController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        playerRigidbody = interactor.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
        }
        
        Debug.Log("Call update player pos");
        // Position player on zipline
        UpdatePlayerPosition();
        
        Debug.Log("Player grabbed zipline! Use arrow keys to move, Space to dismount.");
    }
    
    void SetupLineRenderer()
    {
        // Create or get LineRenderer component
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // Configure LineRenderer
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        
        // Set material if provided
        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }
        else
        {
            // Use default material
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.gray;
            lineRenderer.endColor = Color.gray;
        }
    }
    
    void CalculateZiplineEndpoints()
    {
        if (startPoint != null && endPoint != null)
        {
            ziplineStartPoint = startPoint.position;
            ziplineEndPoint = endPoint.position;
        }
        else
        {
            Debug.LogError("Zipline start or end point not assigned!");
            ziplineStartPoint = transform.position;
            ziplineEndPoint = transform.position + Vector3.forward * 10f;
        }
        
        ziplineDirection = (ziplineEndPoint - ziplineStartPoint).normalized;
        ziplineLength = Vector3.Distance(ziplineStartPoint, ziplineEndPoint);
    }
    
    void UpdateLineRenderer()
    {
        if (lineRenderer != null && startPoint != null && endPoint != null)
        {
            lineRenderer.SetPosition(0, startPoint.position);
            lineRenderer.SetPosition(1, endPoint.position);
        }
    }
    
    void HandleZiplineMovement()
    {
        if (currentPlayer == null || ziplineLength <= 0)
        {
            DismountZipline();
            return;
        }
        
        float moveInput = 0f;
        
        Vector2 inputVector = _playerLocomotionInput.MovementInput;
                
        // Convert input to a world-space direction based on camera
        if (headCamera != null && inputVector.sqrMagnitude > 0.01f) 
        {
            // Get camera forward and right (flattened to horizontal plane)
            Vector3 cameraForward = headCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
                    
            Vector3 cameraRight = headCamera.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();
                    
            // Calculate desired movement direction in world space
            Vector3 desiredDirection = (cameraForward * inputVector.y + cameraRight * inputVector.x).normalized;
                    
            // Project desired direction onto the zipline direction
            // move forward or backward along the zipline
            float dotProduct = Vector3.Dot(desiredDirection, ziplineDirection);
                
            // Use the dot product as move input (-1 to 1)
            moveInput = dotProduct;
        }
        
        // Update progress along the zipline
        float progressDelta = (ziplineSpeed / ziplineLength) * moveInput * Time.deltaTime;
        ziplineProgress += progressDelta;
        
        // Clamp progress to stay on the zipline
        ziplineProgress = Mathf.Clamp01(ziplineProgress);
        
        UpdatePlayerPosition();
        
        // Make player face the direction they're moving
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            Vector3 faceDirection = ziplineDirection * Mathf.Sign(moveInput);
            if (faceDirection != Vector3.zero)
            {
                currentPlayer.transform.rotation = Quaternion.Lerp(
                    currentPlayer.transform.rotation,
                    Quaternion.LookRotation(faceDirection),
                    Time.deltaTime * 10f
                );
            }
        }
    }
    
    void UpdatePlayerPosition()
    {
        Vector3 ziplinePosition = Vector3.Lerp(ziplineStartPoint, ziplineEndPoint, ziplineProgress);
        float hangingOffset = -(playerHeight + handReachOffset);
        Vector3 hangingPosition = ziplinePosition + Vector3.up * hangingOffset;
        currentPlayer.transform.position = hangingPosition;
    }

    void DismountZipline()
    {
        if (currentPlayer == null)
        {
            isRiding = false;
            isDismounting = false;
            return;
        }
        
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
        }
        
        currentPlayer = null;
        _playerLocomotionInput = null;
        playerController = null;
        playerRigidbody = null;

        isRiding = false;
        isDismounting = false;
        Debug.Log("End of DismountZipline");
    }
    
    void OnDrawGizmos()
    {
        // Draw gizmos in editor even when not playing
        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
            Gizmos.DrawWireSphere(startPoint.position, 0.5f);
            Gizmos.DrawWireSphere(endPoint.position, 0.5f);
            
            // Draw direction arrows
            Vector3 direction = (endPoint.position - startPoint.position).normalized;
            Vector3 midPoint = (startPoint.position + endPoint.position) / 2f;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(midPoint, direction * 2f);
            Gizmos.DrawRay(midPoint, -direction * 2f);
        }
        else
        {
            // Draw warning if points not assigned
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
    
}
