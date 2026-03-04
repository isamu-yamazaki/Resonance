using System;
using PurrNet;
using Resonance.Player;
using UnityEngine;
using Unity.Cinemachine;

namespace Resonance.PlayerController
{
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        #region Class Variables
        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private CinemachineCamera _virtualCamera;
        
        private PlayerStats _playerStats;
        
        public float RotationMismatch { get; private set; } = 0f;
        public bool IsRotatingToTarget { get; private set; } = false;
        public static PlayerController LocalPlayer { get; private set; }

        [Header("Base Movement")] 
        public float baseCrouchAcceleration = 25f;
        public float baseCrouchSpeed = 2f;
        public float baseRunAcceleration = 35f;
        public float baseRunSpeed = 4f;
        public float baseSprintAcceleration = 50f;
        public float baseSprintSpeed = 7f;
        public float baseInAirAcceleration = 25f;
        public float drag = 20f;
        public float gravity = 25f;
        public float terminalVelocity = 50f;
        public float jumpSpeed = 1.0f;
        public float movingThreshold = 0.01f;

		[Header("Slide Settings")]
        public float baseSlideSpeed = 8f;
        public float baseMinSlideSpeed = 2f;
        
		public float slideDuration = 1f;
        public float slideDeceleration = 8f;
        public float slopeAngleThreshold = 15f;
        public float uphillSlideDecelerationMultiplier = 2f;
        public float downhillSlideSpeedBoost = 1.5f;

        [Header("Animation")] 
        public float playerModelRotationSpeed = 10f;
        public float rotateToTargetTime = 0.67f; 

        [Header("Camera Settings")]
        public float lookSensitivityH = 0.1f;
        public float lookSensitivityV = 0.1f;
        public float lookLimitV = 89f;
        public float baseFOV = 75f;
        public float sprintFOV = 90f;
        public float overdriveFOV = 110f;
        public float fovTransitionSpeed = 10f;

        [Header("Environment Details")] 
        [SerializeField] private LayerMask _groundLayers;
        
        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;
        private OverdriveAbility _overdriveAbility;
        
        // Death flag to immediately block all movement
        public bool IsPlayerDead { get; set; } = false;
        
        //Current speed stats - get updated during runtime
        private float crouchAcceleration;
        private float crouchSpeed;
        private float runAcceleration;
        private float runSpeed;
        private float sprintAcceleration;
        private float sprintSpeed;
        private float inAirAcceleration;
        private float slideSpeed;
        private float minSlideSpeed;
        
        private Vector2 _cameraRotation = Vector2.zero;
        private Vector2 _playerTargetRotation = Vector2.zero;

        private bool _jumpedLastFrame = false;
        private bool _isRotatingClockwise = false;
        private float _rotatingToTargetTimer = 0f;
        private float _verticalVelocity = 0f;
        private float _antiBump;
        private float _stepOffset;
        
        // Slide variables
        private bool _wasCrouchPressedLastFrame = false;
        private float _slideTimer = 0f;
        private Vector3 _slideDirection = Vector3.zero;

        private PlayerMovementState _lastMovementState = PlayerMovementState.Falling;

        #endregion

        #region Startup
        protected override void OnSpawned()
        {
            base.OnSpawned();
            
            if (isOwner)
            {
                LocalPlayer = this;
            }

            enabled = isOwner;
            _virtualCamera.gameObject.SetActive(isOwner);

            if (isOwner && _playerLocomotionInput != null)
            {
                PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.Enable();
                PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.SetCallbacks(_playerLocomotionInput);
            }
        }

        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();
            _overdriveAbility = GetComponent<OverdriveAbility>();
            _playerStats = GetComponent<PlayerStats>();

            _antiBump = sprintSpeed;
            _stepOffset = _characterController.stepOffset;

            if (_virtualCamera != null)
                _virtualCamera.Lens.FieldOfView = baseFOV;
        }
        #endregion

        #region Public Methods
        public void ResetState()
        {
            _verticalVelocity = 0f;
            _slideTimer = 0f;
            _slideDirection = Vector3.zero;
            _jumpedLastFrame = false;
            _wasCrouchPressedLastFrame = false;
            _rotatingToTargetTimer = 0f;
            IsRotatingToTarget = false;
            
            // Restore step offset to original value
            if (_characterController != null)
            {
                _characterController.stepOffset = _stepOffset;
            }
        }
        #endregion

        #region Update Logic
        private void Update()
        {
            // Immediately return if player is dead (flag-based check)
            if (IsPlayerDead)
                return;
            
            // Don't process movement if player is dead
            if (_playerState.IsDead())
                return;
            
            // Don't process movement if CharacterController is disabled
            if (!_characterController.enabled)
                return;
            
            UpdateStats();
            UpdateMovementState();
            HandleVerticalMovement();
            HandleLateralMovement();
        }
        
        private void UpdateStats()
        {
            float speedMult = _playerStats.PlayerSpeed;
            
            crouchSpeed = baseCrouchSpeed * speedMult;
            runSpeed = baseRunSpeed * speedMult;
            sprintSpeed = baseSprintSpeed * speedMult;
            slideSpeed = baseSlideSpeed * speedMult;
            minSlideSpeed = baseMinSlideSpeed * speedMult;
            
            crouchAcceleration = baseCrouchAcceleration * speedMult;
            runAcceleration = baseRunAcceleration * speedMult;
            sprintAcceleration = baseSprintAcceleration * speedMult;
            inAirAcceleration = baseInAirAcceleration * speedMult;
            
            _antiBump = sprintSpeed * speedMult;
        }
        
        private void UpdateMovementState()
        {
            _lastMovementState = _playerState.CurrentPlayerMovementState;
            
			// order matters
            bool canRun = CanRun();
            bool isMovementInput = _playerLocomotionInput.MovementInput != Vector2.zero;
            bool isMovingLaterally = IsMovingLaterally();
            bool isGrounded = IsGrounded();
            bool isCrouchToggled = _playerLocomotionInput.CrouchToggledOn;
			bool isSprinting = _playerLocomotionInput.SprintToggledOn && isMovingLaterally && !isCrouchToggled && canRun;
            
            // Check for slide initiation
            bool crouchJustPressed = isCrouchToggled && !_wasCrouchPressedLastFrame;
            bool isCurrentlySprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isCurrentlySliding = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sliding;
            
            // Initiate slide: crouch pressed while sprinting and grounded
            if (crouchJustPressed && isCurrentlySprinting && isGrounded)
            {
                _slideTimer = slideDuration;
                _slideDirection = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).normalized;
                _playerState.SetPlayerMovementState(PlayerMovementState.Sliding);
                _wasCrouchPressedLastFrame = isCrouchToggled;
                _characterController.stepOffset = _stepOffset;
                return;
            }

            _wasCrouchPressedLastFrame = isCrouchToggled;
            
            // Handle active slide
            if (isCurrentlySliding)
            {
                // Exit slide conditions: crouch released, airborne, or jump pressed
                bool shouldEndSlide = !isCrouchToggled || !isGrounded || _playerLocomotionInput.JumpPressed;
        
                if (shouldEndSlide)
                {
                    _slideTimer = 0f;
                    _playerLocomotionInput.DisableCrouch();
            
                    // Jump out of slide
                    if (_playerLocomotionInput.JumpPressed && isGrounded)
                    {
                        _verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
                        _jumpedLastFrame = true;
                    }
            
                    // Fall through to determine new state
                }
                else
                {
                    _playerState.SetPlayerMovementState(PlayerMovementState.Sliding);
                    return;
                }
            }
            
            // Determine ground movement state
            PlayerMovementState lateralState = isCrouchToggled ? PlayerMovementState.Crouching : 
                                               isSprinting ? PlayerMovementState.Sprinting :  
                                               isMovingLaterally || isMovementInput ? PlayerMovementState.Running : PlayerMovementState.Idling;

            _playerState.SetPlayerMovementState(lateralState);
            
            // Control Airborne State
            if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y > 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                _jumpedLastFrame = false;
                _characterController.stepOffset = 0f;
            }
            else if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y <= 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Falling);
                _jumpedLastFrame = false;
                _characterController.stepOffset = 0f;
            }
            else
            {
                _characterController.stepOffset = _stepOffset;
            }
        }

        private void HandleVerticalMovement()
        {
            bool isGrounded = _playerState.InGroundedState();
            
            _verticalVelocity -= gravity * Time.deltaTime;

            if (isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -_antiBump;

            if (_playerLocomotionInput.JumpPressed && isGrounded)
            {
                _verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
                _jumpedLastFrame = true;
            }

            if (_playerState.IsStateGroundedState(_lastMovementState) && !isGrounded)
            {
                _verticalVelocity += _antiBump;
            }

            if (Mathf.Abs(_verticalVelocity) > Mathf.Abs(terminalVelocity))
            {
                _verticalVelocity = -1f * Mathf.Abs(terminalVelocity);
            }
        }

        private void HandleLateralMovement()
        {
            bool isSliding = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sliding;

            if (isSliding)
            {
                HandleSlideMovement();
                return;
            }
            
			bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = _playerState.InGroundedState();
            bool isCrouching = _playerState.CurrentPlayerMovementState == PlayerMovementState.Crouching;
			
			// State dependent acceleration and speed
			float lateralAcceleration = !isGrounded ? inAirAcceleration :
                                        isCrouching ? crouchAcceleration :
                                        isSprinting ? sprintAcceleration : runAcceleration;
			float clampLateralMagnitude = !isGrounded ? sprintSpeed : 
                                          isCrouching ? crouchSpeed :
                                          isSprinting ? sprintSpeed : runSpeed;

            Vector3 cameraForwardXZ = new Vector3(_virtualCamera.transform.forward.x, 0f, _virtualCamera.transform.forward.z).normalized;
            Vector3 cameraRightXZ = new Vector3(_virtualCamera.transform.right.x, 0f, _virtualCamera.transform.right.z).normalized;
            Vector3 movementDirection = cameraRightXZ * _playerLocomotionInput.MovementInput.x + cameraForwardXZ * _playerLocomotionInput.MovementInput.y;
            
            Vector3 movementDelta = movementDirection * lateralAcceleration * Time.deltaTime;
            Vector3 newVelocity = _characterController.velocity + movementDelta;
            
            // Add drag to player
            Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;
            newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
            newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), clampLateralMagnitude);
            newVelocity.y = _verticalVelocity;
            newVelocity = !isGrounded ? HandleSteepWalls(newVelocity) : newVelocity;
            
            // Move character (Unity suggests only calling this once per tick)
            _characterController.Move(newVelocity * Time.deltaTime);
        }

        private void HandleSlideMovement()
        {
            Vector3 groundNormal = CharacterControllerUtils.GetNormalWithSphereCast(_characterController, _groundLayers);
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
    
            Vector3 slopeDownDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
    
            float slopeDot = Vector3.Dot(_slideDirection, slopeDownDirection);
    
            bool isDownhill = slopeAngle > slopeAngleThreshold && slopeDot > 0.1f;
            bool isUphill = slopeAngle > slopeAngleThreshold && slopeDot < -0.1f;
    
            // Update slide timer based on slope
            if (isDownhill)
            {
                _slideTimer -= Time.deltaTime * 0.5f; // Slower decay on downhill
            }
            else if (isUphill)
            {
                _slideTimer -= Time.deltaTime * uphillSlideDecelerationMultiplier;
            }
            else
            {
                _slideTimer -= Time.deltaTime;
            }
    
            // Calculate slide speed
            float slideProgress = 1f - (_slideTimer / slideDuration);
            float currentSlideSpeed = Mathf.Lerp(slideSpeed, minSlideSpeed, slideProgress);
    
            // Apply slope modifications to speed
            if (isDownhill)
            {
                currentSlideSpeed *= downhillSlideSpeedBoost;
            }
            else if (isUphill)
            {
                currentSlideSpeed = Mathf.Max(currentSlideSpeed - (slideDeceleration * uphillSlideDecelerationMultiplier * Time.deltaTime), minSlideSpeed);
            }
            else
            {
                currentSlideSpeed = Mathf.Max(currentSlideSpeed - (slideDeceleration * Time.deltaTime), minSlideSpeed);
            }
            
            // Apply Overdrive speed multiplier to slide
            if (_overdriveAbility != null && _overdriveAbility.IsInOverdrive)
            {
                currentSlideSpeed *= _overdriveAbility.SpeedMultiplier;
            }
    
            // End slide when timer expires
            if (_slideTimer <= 0f)
            {
                _playerLocomotionInput.DisableCrouch();
                return;
            }
    
            // Move in locked slide direction
            Vector3 slideVelocity = _slideDirection * currentSlideSpeed;
            slideVelocity.y = _verticalVelocity;
    
            _characterController.Move(slideVelocity * Time.deltaTime);
        }

        private Vector3 HandleSteepWalls(Vector3 velocity)
        {
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(_characterController, _groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            bool validAngle = angle <= _characterController.slopeLimit;

            if (!validAngle && _verticalVelocity < 0f)
                velocity = Vector3.ProjectOnPlane(velocity, normal);
            
            return velocity;
        }
        #endregion
        
        #region Late Update Logic
        private void LateUpdate()
        {
            // Don't process if player is dead
            if (IsPlayerDead)
                return;
                
            UpdateCameraRotation();
            UpdateCameraFOV();
        }

        private void UpdateCameraFOV()
        {
            if (_virtualCamera == null) return;
            
            // Determine target FOV based on state priority: Overdrive > Sprint > Base
            float targetFOV = baseFOV;
            
            bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isOverdriveActive = _overdriveAbility != null && _overdriveAbility.IsInOverdrive;

            if (isOverdriveActive)
            {
                targetFOV = overdriveFOV;
            }
            else if (isSprinting)
            {
                targetFOV = sprintFOV;
            }
            
            _virtualCamera.Lens.FieldOfView = Mathf.Lerp(_virtualCamera.Lens.FieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
        }

        private void UpdateCameraRotation()
        {
            _cameraRotation.x += lookSensitivityH * _playerLocomotionInput.LookInput.x;
            _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSensitivityV * _playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);
            
            _playerTargetRotation.x += transform.eulerAngles.x + lookSensitivityH * _playerLocomotionInput.LookInput.x;
            
            float rotationTolerance = 90f;
            bool isIdling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
            IsRotatingToTarget = _rotatingToTargetTimer > 0f;
            
            // ROTATE if we're not idling
            if (!isIdling)
            {
                RotatePlayerToTarget();
            }
            // If rotation mismatch not within tolerance, or rotate to target is active, ROTATE
            else if (Mathf.Abs(RotationMismatch) > rotationTolerance || IsRotatingToTarget)
            {
                UpdateIdleRotation(rotationTolerance);
            }
            
            _virtualCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);
            
            // Get angle between camera and player
            Vector3 camForwardProjectedXZ = new Vector3(_virtualCamera.transform.forward.x, 0f, _virtualCamera.transform.forward.z).normalized;
            Vector3 crossProduct = Vector3.Cross(transform.forward, camForwardProjectedXZ);
            float sign = Mathf.Sign(Vector3.Dot(crossProduct, transform.up));
            RotationMismatch = sign * Vector3.Angle(transform.forward, camForwardProjectedXZ);
        }

        private void UpdateIdleRotation(float rotationTolerance)
        {
            // Initiate new rotation direction
            if (Mathf.Abs(RotationMismatch) > rotationTolerance)
            {
                _rotatingToTargetTimer = rotateToTargetTime;
                _isRotatingClockwise = RotationMismatch > rotationTolerance;
            }
            _rotatingToTargetTimer -= Time.deltaTime;
            
            // Rotate player
            if (_isRotatingClockwise && RotationMismatch > 0f ||
                !_isRotatingClockwise && RotationMismatch < 0f)
            {
                RotatePlayerToTarget();
            }
        }

        private void RotatePlayerToTarget()
        {
            Quaternion targetRotationX = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotationX, playerModelRotationSpeed * Time.deltaTime);
        }
        #endregion

        #region State Checks
        private bool IsMovingLaterally()
        {
            Vector3 lateralVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
            
            return lateralVelocity.magnitude > movingThreshold;
        }

        private bool IsGrounded()
        {
            bool grounded = _playerState.InGroundedState() ? IsGroundedWhileGrounded() : IsGroundedWhileAirborne();
            
            return grounded;
        }

        private bool IsGroundedWhileGrounded()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _characterController.radius, transform.position.z);

            bool grounded = Physics.CheckSphere(spherePosition, _characterController.radius, _groundLayers, QueryTriggerInteraction.Ignore);
            
            return grounded;
        }

        private bool IsGroundedWhileAirborne()
        {
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(_characterController, _groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            bool validAngle = angle <= _characterController.slopeLimit;
            
            return _characterController.isGrounded && validAngle;
        }
        
        private bool CanRun()
        {
            // This means player is moving diagonally at 45 degrees or forward, if so, we can run
            return _playerLocomotionInput.MovementInput.y >= MathF.Abs(_playerLocomotionInput.MovementInput.x);
        }
        #endregion
    }
}
