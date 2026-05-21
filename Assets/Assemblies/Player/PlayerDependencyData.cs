using UnityEngine;

namespace Resonance.Assemblies.Player
{
    /// <summary>
    /// Per-tick snapshot of values sourced from server-synced dependencies
    /// (PlayerStats, PlayerState). The simulation reads from this snapshot
    /// rather than reaching into the live MonoBehaviours, keeping the tick
    /// logic stateless and pure.
    /// </summary>
    public struct PlayerDependencyData
    {
        /// <summary>From PlayerStats.PlayerSpeed (predicted state).</summary>
        public float MovementSpeedMultiplier;

        /// <summary>From PlayerState.CurrentPlayerMovementState (ValidatedSyncVar&lt;int&gt;).</summary>
        public PlayerMovementState CurrentPlayerMovementState;
        public Vector3 TrainVelocityOffset;
        public float TrainKnockbackVertical;
        public LayerMask GroundLayers;
        public float OverdriveSpeedMultiplier;
        public bool IsInOverdrive;
    }
}
