namespace Resonance.Assemblies.Player
{
    public static class PlayerMovementStateUtils
    {
        public static bool IsStateGroundedState(PlayerMovementState movementState)
        {
            return movementState == PlayerMovementState.Idling ||
                   movementState == PlayerMovementState.Crouching ||
                   movementState == PlayerMovementState.Running ||
                   movementState == PlayerMovementState.Sprinting ||
                   movementState == PlayerMovementState.Sliding;
        }
    }

}
