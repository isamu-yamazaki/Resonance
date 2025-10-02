using UnityEngine;

namespace Resonance.PlayerController
{
    public class PlayerState : MonoBehaviour
    {
        [field: SerializeField]
        public PlayerMovementState CurrentPlayerMovementState { get; private set; } = PlayerMovementState.Idling;

        public void SetPlayerMovementState(PlayerMovementState playerMovementState)
        {
            CurrentPlayerMovementState = playerMovementState;
        }

        public bool InGroundedState()
        {
            return CurrentPlayerMovementState == PlayerMovementState.Idling ||
                   CurrentPlayerMovementState == PlayerMovementState.Crouching ||
                   CurrentPlayerMovementState == PlayerMovementState.Running ||
                   CurrentPlayerMovementState == PlayerMovementState.Sprinting ||
                   CurrentPlayerMovementState == PlayerMovementState.Sliding;

        }
    }
    
    public enum PlayerMovementState
    {
        Idling = 0,
        Crouching = 1,
        Running = 2,
        Sprinting = 3,
        Jumping = 4,
        Falling = 5,
        Sliding = 6,
        Strafing = 7,
    }
}
