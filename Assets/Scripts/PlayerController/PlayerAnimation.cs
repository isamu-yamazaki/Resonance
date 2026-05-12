using System.Linq;
using PurrNet;
using Resonance.Assemblies.Player;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private NetworkAnimator _networkAnimator;
        [SerializeField] private float locomotionBlendSpeed = 4f;

        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;
        private PlayerController _playerController;
        private PlayerActionsInput _playerActionsInput;

        private static int inputXHash = Animator.StringToHash("inputX");
        private static int inputYHash = Animator.StringToHash("inputY");
        private static int isIdlingHash = Animator.StringToHash("isIdling");
        private static int isGroundedHash = Animator.StringToHash("isGrounded");
        private static int isFallingHash = Animator.StringToHash("isFalling");
        private static int isJumpingHash = Animator.StringToHash("isJumping");
        private static int isCrouchingHash = Animator.StringToHash("isCrouching");
        private static int isSlidingHash = Animator.StringToHash("isSliding");
        private static int isAttackingHash = Animator.StringToHash("isAttacking");
        private static int isReloadingHash = Animator.StringToHash("isReloading");
        private static int isPlayingActionHash = Animator.StringToHash("isPlayingAction");
        private static int weaponClassHash = Animator.StringToHash("weaponClass");
        private static int weaponClassInitializedHash = Animator.StringToHash("weaponClassInitialized");
        private static int verticalAimHash = Animator.StringToHash("verticalAim");

        private int[] actionHashes;
        private Vector3 _currentBlendInput = Vector3.zero;

        private void Awake()
        {
            _playerLocomotionInput = PlayerLocomotionInput.Instance;
            _playerState = GetComponent<PlayerState>();
            _playerController = GetComponent<PlayerController>();
            _playerActionsInput = PlayerActionsInput.Instance;

            actionHashes = new int[] { isAttackingHash, isReloadingHash };
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
            bool isSliding = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sliding;
            bool isJumping = _playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping;
            bool isFalling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Falling;
            bool isGrounded = _playerState.InGroundedState();
            bool isPlayingAction = actionHashes.Any(hash => _animator.GetBool(hash));

            Vector2 inputTarget = isSliding ? Vector2.zero :
                                  isSprinting ? _playerLocomotionInput.MovementInput * 1.5f :
                                  isRunning ? _playerLocomotionInput.MovementInput * 1f :
                                  _playerLocomotionInput.MovementInput * 0.5f;

            _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);

            Vector2 clampedInput = new Vector2(
                Mathf.Abs(_currentBlendInput.x) < 0.1f ? 0f : _currentBlendInput.x,
                Mathf.Abs(_currentBlendInput.y) < 0.1f ? 0f : _currentBlendInput.y
            );

            float pitch = Camera.main != null ? Camera.main.transform.localEulerAngles.x : 0f;
            if (pitch > 180f) pitch -= 360f;
            float verticalAim = pitch / 90f;

            _networkAnimator.SetBool(isGroundedHash, isGrounded);
            _networkAnimator.SetBool(isIdlingHash, isIdling);
            _networkAnimator.SetBool(isFallingHash, isFalling);
            _networkAnimator.SetBool(isJumpingHash, isJumping);
            _networkAnimator.SetBool(isCrouchingHash, isCrouching);
            _networkAnimator.SetBool(isSlidingHash, isSliding);
            _networkAnimator.SetBool(isAttackingHash, _playerState.IsAttacking);
            _networkAnimator.SetBool(isReloadingHash, _playerState.IsReloading);
            _networkAnimator.SetBool(isPlayingActionHash, isPlayingAction);
            _networkAnimator.SetBool(weaponClassInitializedHash, _playerState.WeaponClassInitialized);
            _networkAnimator.SetInt(weaponClassHash, (int)_playerState.CurrentWeaponClass);
            _networkAnimator.SetFloat(inputXHash, clampedInput.x);
            _networkAnimator.SetFloat(inputYHash, clampedInput.y);
            _networkAnimator.SetFloat(verticalAimHash, verticalAim);
        }
    }
}
