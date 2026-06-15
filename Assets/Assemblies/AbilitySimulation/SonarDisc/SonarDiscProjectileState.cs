using PurrNet;
using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Assemblies.AbilitySimulation.SonarDisc
{
    public struct SonarDiscProjectileState : IPredictedData<SonarDiscProjectileState>
    {
        public Vector3 LastPosition;

        public bool IsAttached;

        /// <summary>
        /// True if attached to a player, false if attached to another object.
        /// Use in combination with `IsAttached`.
        /// </summary>
        public bool IsAttachedToPlayer => AttachedPlayer.HasValue;

        /// <summary>
        /// Populated if attached to a player, null if attached to another object.
        /// Use in combination with `IsAttached`.
        /// </summary>
        public PlayerID? AttachedPlayer;

        /// <summary>
        /// The predicted object the disc is attached to (e.g. a moving train), or null for static world
        /// geometry. Referenced by id (not a Transform) so it survives rollback/replay and pooling.
        /// </summary>
        public PredictedObjectID? AttachTargetId;

        /// <summary>
        /// Attach pose stored in <see cref="AttachTargetId"/>'s local space; FollowTarget rebuilds the
        /// world pose from the target's reconciled transform each tick.
        /// </summary>
        public Vector3 AttachLocalPos;
        public Quaternion AttachLocalRot;

        public bool IsDespawning;

        /// <summary>
        /// For the view to fire the pulse VFX.
        /// This is fully controlled by the simulation and may run some time
        /// after attaching to a wall, which is why we need the separate property.
        /// </summary>
        public bool IsPulsing;

        public float PrePulseElapsed;
        public float PulseElapsed;
        public float DistanceTravelled;

        /// <summary>
        /// Whether the client should play the shoot sound.
        /// </summary>
        public bool PlayShootSound => TicksUntilShootSound <= 0;

        /// <summary>
        /// Number of server ticks to wait until PlayShootSound is true.
        /// </summary>
        public int TicksUntilShootSound;

        /// <summary>
        /// Per-tick output: true on the tick the disc should be destroyed (max range reached, or the
        /// wall pulse completed). Consumed each tick by the owning behaviour, never accumulated.
        /// </summary>
        public bool ShouldDestroy;

        /// <summary>
        /// Per-tick output: true on a tick the expanding pulse should run a server-side scan.
        /// Consumed each tick, never accumulated.
        /// </summary>
        public bool ShouldScanThisTick;

        /// <summary>
        /// Per-tick output: the pulse radius this tick, used by the external scan to distance-cull
        /// candidates. Valid only when <see cref="ShouldScanThisTick"/> is true.
        /// </summary>
        public float CurrentPulseRadius;

        public void Dispose()
        {
        }
    }
}
