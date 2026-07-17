using System.Linq;
using PurrNet;
using PurrNet.Prediction;
using Resonance.Assemblies.Player;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class PlayerAnimation : PredictedIdentity<PlayerAnimationInput, PlayerAnimationState>
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float locomotionBlendSpeed = 4f;

        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;

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

        #region Lifecycle

        protected override void LateAwake()
        {
            _playerLocomotionInput = PlayerLocomotionInput.Instance;
            _playerState = GetComponent<PlayerState>();

            actionHashes = new int[] { isAttackingHash, isReloadingHash };
        }

        #endregion

        #region Input

        protected override void GetFinalInput(ref PlayerAnimationInput input)
        {
            input.MovementInput = _playerLocomotionInput.MovementInput;
            input.CameraPitch = Camera.main != null ? Camera.main.transform.localEulerAngles.x : 0f;
        }

        #endregion

        #region Simulation

        protected override void SanitizeInput(ref PlayerAnimationInput input)
        {
            if (input.CameraPitch > 180f) input.CameraPitch -= 360f;
        }

        protected override void Simulate(PlayerAnimationInput input, ref PlayerAnimationState state, float delta)
        {
            var movementState = _playerState.CurrentPlayerMovementState;
            Vector2 inputTarget =
                movementState switch
                {
                    PlayerMovementState.Sliding => Vector2.zero,
                    PlayerMovementState.Sprinting => input.MovementInput * 1.5f,
                    PlayerMovementState.Running => input.MovementInput * 1.0f,
                    _ => input.MovementInput * 0.5f
                };

            state.BlendInput = Vector2.Lerp(state.BlendInput, inputTarget, locomotionBlendSpeed * delta);
            state.CameraPitch = input.CameraPitch;
        }

        #endregion

        #region Local view updates

        protected override PlayerAnimationState Interpolate(PlayerAnimationState from, PlayerAnimationState to, float t)
            => new()
            {
                BlendInput = Vector2.Lerp(from.BlendInput, to.BlendInput, t),
                CameraPitch = Mathf.Lerp(from.CameraPitch, to.CameraPitch, t),
            };

        protected override void UpdateView(PlayerAnimationState viewState, PlayerAnimationState? verified)
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

            Vector2 clampedInput = new Vector2(
                Mathf.Abs(viewState.BlendInput.x) < 0.1f ? 0f : viewState.BlendInput.x,
                Mathf.Abs(viewState.BlendInput.y) < 0.1f ? 0f : viewState.BlendInput.y
            );

            float pitch = viewState.CameraPitch;
            float verticalAim = pitch / 90f;

            _animator.SetBool(isGroundedHash, isGrounded);
            _animator.SetBool(isIdlingHash, isIdling);
            _animator.SetBool(isFallingHash, isFalling);
            _animator.SetBool(isJumpingHash, isJumping);
            _animator.SetBool(isCrouchingHash, isCrouching);
            _animator.SetBool(isSlidingHash, isSliding);
            _animator.SetBool(isAttackingHash, _playerState.IsAttacking);
            _animator.SetBool(isReloadingHash, _playerState.IsReloading);
            _animator.SetBool(isPlayingActionHash, isPlayingAction);
            _animator.SetBool(weaponClassInitializedHash, _playerState.WeaponClassInitialized);
            _animator.SetInteger(weaponClassHash, (int)_playerState.CurrentWeaponClass);
            _animator.SetFloat(inputXHash, clampedInput.x);
            _animator.SetFloat(inputYHash, clampedInput.y);
            _animator.SetFloat(verticalAimHash, verticalAim);
        }

        #endregion
    }

    public struct PlayerAnimationInput : IPredictedData
    {
        public Vector2 MovementInput;
        public float CameraPitch;

        public void Dispose()
        {
        }
    }

    public struct PlayerAnimationState : IPredictedData<PlayerAnimationState>
    {
        public Vector2 BlendInput;
        public float CameraPitch;

        public void Dispose()
        {
        }
    }
}