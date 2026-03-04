using Resonance.PlayerController;
using UnityEngine;

public class Zipline : MonoBehaviour, IInteractable
{
    public Collider InteractRange { get; set; }

    [SerializeField] public Transform startPoint;
    [SerializeField] public Transform endPoint;
    public float ziplineSpeed = 10f;
    private PlayerLocomotionInput _playerLocomotionInput;
    
    public float lineWidth = 0.1f;
    public Material lineMaterial;
    private LineRenderer lineRenderer;
    private GameObject currentPlayer;
    private bool isRiding = false;
    private float ziplineProgress = 0f;
    private CharacterController playerController;
    private Rigidbody playerRigidbody;
    private Vector3 ziplineDirection;
    private float ziplineLength;
    private Vector3 ziplineStartPoint;
    private Vector3 ziplineEndPoint;
    
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
        
        if (isRiding)
        {
            HandleZiplineMovement();
            
            // Check for dismount
            if (_playerLocomotionInput.JumpPressed)
            {
                DismountZipline();
            }
        }
    }
    
    public void Interact(GameObject interactor)
    {
        //code for going on da zipline
        //use player interactor to do anything needed to player

        // Start riding the zipline
        currentPlayer = interactor;
        isRiding = true;
        
        // Recalculate in case points moved
        CalculateZiplineEndpoints();
        
        // Find the closest point on the zipline to attach the player
        Vector3 playerPos = interactor.transform.position;
        Vector3 ziplineVector = ziplineEndPoint - ziplineStartPoint;
        
        // Project player position onto the zipline
        float projectionLength = Vector3.Dot(playerPos - ziplineStartPoint, ziplineVector.normalized);
        projectionLength = Mathf.Clamp(projectionLength, 0f, ziplineLength);
        
        // Set initial progress based on where player grabbed the zipline
        ziplineProgress = projectionLength / ziplineLength;
        
        // Disable player controller if it exists
        playerController = interactor.GetComponent<CharacterController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Disable player rigidbody if it exists
        playerRigidbody = interactor.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
        }
        
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
        
        // Get input based on control scheme
        float moveInput = 0f;
      
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.D))
            moveInput = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A))
            moveInput = -1f;
        
        // Update progress along the zipline
        float progressDelta = (ziplineSpeed / ziplineLength) * moveInput * Time.deltaTime;
        ziplineProgress += progressDelta;
        
        // Clamp progress to stay on the zipline
        ziplineProgress = Mathf.Clamp01(ziplineProgress);
        
        // Update player position and rotation
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
        Vector3 newPosition = Vector3.Lerp(ziplineStartPoint, ziplineEndPoint, ziplineProgress);
        currentPlayer.transform.position = newPosition;
    }
    
    void DismountZipline()
    {
        if (currentPlayer == null)
        {
            isRiding = false;
            return;
        }
        
        // Re-enable player controller
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Re-enable player rigidbody
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
        }
        
        isRiding = false;
        
        Debug.Log("Dismounted from zipline!");
        
        currentPlayer = null;
        playerController = null;
        playerRigidbody = null;
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
