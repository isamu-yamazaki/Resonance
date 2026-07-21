namespace Resonance.Assemblies.Player
{
    /// <summary>
    /// Per-tick derived movement stats. Recomputed each simulation tick from
    /// PlayerInputData.MovementSpeedMultiplier and PlayerConfig.base* values.
    /// Mirrors the cached fields in legacy PlayerController.UpdateStats().
    /// </summary>
    public struct PlayerDerivedStats
    {
        public float crouchSpeed;
        public float runSpeed;
        public float sprintSpeed;
        public float slideSpeed;
        public float minSlideSpeed;

        public float crouchAcceleration;
        public float runAcceleration;
        public float sprintAcceleration;
        public float inAirAcceleration;

        public float drag;

        // Anti-bump pulls toward ground when grounded; legacy code sets it to sprintSpeed.
        public float antiBump;
    }
}
