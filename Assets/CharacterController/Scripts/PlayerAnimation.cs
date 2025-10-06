using System.Linq;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float locomotionBlendSpeed = 4f;
        
        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;
        private PlayerController _playerController;
        private PlayerActionsInput _playerActionsInput;
        
        // Locomotion
        private static int inputXHash = Animator.StringToHash("inputX");
        private static int inputYHash = Animator.StringToHash("inputY");
        private static int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");
        private static int isIdlingHash = Animator.StringToHash("isIdling");
        private static int isGroundedHash = Animator.StringToHash("isGrounded");
        private static int isFallingHash = Animator.StringToHash("isFalling");
        private static int isJumpingHash = Animator.StringToHash("isJumping");
        private static int isCrouchingHash = Animator.StringToHash("isCrouching");
        
        // Actions
        private static int isAttackingHash = Animator.StringToHash("isAttacking");
        private static int isReloadingHash = Animator.StringToHash("isReloading");
        private static int isPlayingActionHash = Animator.StringToHash("isPlayingAction");
        private int[] actionHashes;
        
        // Camera/Rotation
        private static int isRotatingToTargetHash = Animator.StringToHash("isRotatingToTarget");
        private static int rotationMismatchHash = Animator.StringToHash("rotationMismatch");
        
        private Vector3 _currentBlendInput = Vector3.zero;

        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();
            _playerController = GetComponent<PlayerController>();
            _playerActionsInput = GetComponent<PlayerActionsInput>();

            actionHashes = new int[] { }; // interruptible actions go here
        }

        private void Update()
        {
            UpdateAnimationState();
        }

        private void UpdateAnimationState()
        {
            bool isIdling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
            bool isRunning = _playerState.CurrentPlayerMovementState == PlayerMovementState.Running;
            bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isCrouching = _playerState.CurrentPlayerMovementState == PlayerMovementState.Crouching;
            bool isJumping = _playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping;
            bool isFalling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Falling;
            bool isGrounded = _playerState.InGroundedState();
            bool isPlayingAction = actionHashes.Any(hash => _animator.GetBool(hash));

            Vector2 inputTarget = isSprinting ? _playerLocomotionInput.MovementInput * 1.5f : 
                                  isRunning ? _playerLocomotionInput.MovementInput * 1f : _playerLocomotionInput.MovementInput * 0.5f;
            _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);
            
            _animator.SetBool(isGroundedHash, isGrounded);
            _animator.SetBool(isIdlingHash, isIdling);
            _animator.SetBool(isFallingHash, isFalling);
            _animator.SetBool(isJumpingHash, isJumping);
            _animator.SetBool(isCrouchingHash, isCrouching);
            _animator.SetBool(isRotatingToTargetHash, _playerController.IsRotatingToTarget);
            _animator.SetBool(isAttackingHash, _playerActionsInput.AttackPressed);
            _animator.SetBool(isReloadingHash, _playerActionsInput.ReloadPressed);
            _animator.SetBool(isPlayingActionHash, isPlayingAction);
            
            _animator.SetFloat(inputXHash, _currentBlendInput.x);
            _animator.SetFloat(inputYHash, _currentBlendInput.y);
            _animator.SetFloat(inputMagnitudeHash, _currentBlendInput.magnitude);
            _animator.SetFloat(rotationMismatchHash, _playerController.RotationMismatch);
        }
    }
}
