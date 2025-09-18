using System;
using UnityEngine;

namespace Resonance.PlayerController
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        #region Class Variables
        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Camera _playerCamera;
        
        [Header("Base Movement")]
        public float runAcceleration = 0.25f;
        public float runSpeed = 4f;
        public float drag = 0.1f;
        public float movingThreshold = 0.01f;

        [Header("Camera Settings")]
        public float lookSensitivityH = 0.1f;
        public float lookSensitivityV = 0.1f;
        public float lookLimitV = 89f;
        
        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;
        
        private Vector2 _cameraRotation = Vector2.zero;
        private Vector2 _playerTargetRotation = Vector2.zero;
        #endregion

        #region Startup
        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();
        }
        #endregion

        #region Update Logic
        private void Update()
        {
            UpdateMovementState();
            HandleLateralMovement();
        }

        private void UpdateMovementState()
        {
            bool isMovementInput = _playerLocomotionInput.MovementInput != Vector2.zero;
            bool isMovingLaterally = IsMovingLaterally();
            
            PlayerMovementState lateralState = isMovingLaterally || isMovementInput ? PlayerMovementState.Running : PlayerMovementState.Idling;
            _playerState.SetPlayerMovementState(lateralState);
        }

        private void HandleLateralMovement()
        {
            Vector3 cameraForwardXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized;
            Vector3 cameraRightXZ = new Vector3(_playerCamera.transform.right.x, 0f, _playerCamera.transform.right.z).normalized;
            Vector3 movementDirection = cameraRightXZ * _playerLocomotionInput.MovementInput.x + cameraForwardXZ * _playerLocomotionInput.MovementInput.y;
            
            Vector3 movementDelta = movementDirection * runAcceleration *  Time.deltaTime;
            Vector3 newVelocity = _characterController.velocity + movementDelta;
            
            // Add drag to player
            Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;
            newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
            newVelocity = Vector3.ClampMagnitude(newVelocity, runSpeed);
            
            // Move character (Unity suggests only calling this once per tick)
            _characterController.Move(newVelocity * Time.deltaTime);
        }
        #endregion
        
        #region Late Update Logic
        private void LateUpdate()
        {
            _cameraRotation.x += lookSensitivityH * _playerLocomotionInput.LookInput.x;
            _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSensitivityV * _playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);
            
            _playerTargetRotation.x += transform.eulerAngles.x + lookSensitivityH * _playerLocomotionInput.LookInput.x;
            transform.rotation = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);
            
            _playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);
        }
        #endregion

        #region State Checks
        private bool IsMovingLaterally()
        {
            Vector3 lateralVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
            
            return lateralVelocity.magnitude > movingThreshold;
        }
        #endregion
    }
}
