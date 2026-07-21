using UnityEngine.Scripting.APIUpdating;

namespace Resonance.Assemblies.Player
{
    [MovedFrom(true, "Resonance.PlayerController", null, "PlayerMovementState")]
    public enum PlayerMovementState
    {
        Idling = 0,
        Crouching = 1,
        Running = 2,
        Sprinting = 3,
        Jumping = 4,
        Falling = 5,
        Sliding = 6,
        Dead = 7,
        Ziplining = 8,
        PreMatchFrozen = 9,
        MatchEndedFrozen = 10,
        Grappling = 11,
    }
}
