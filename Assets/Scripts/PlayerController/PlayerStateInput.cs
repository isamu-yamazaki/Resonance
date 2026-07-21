using PurrNet.Prediction;
using Resonance.Assemblies.Player;
using Resonance.Combat.Weapons.Enums;

namespace Resonance.PlayerController
{
    public struct PlayerStateInput : IPredictedData
    {
        public bool RequestExternalPlayerMovementStateUpdate;
        public bool RequestExternalWeaponStateUpdate;

        public PlayerMovementState RequestedPlayerMovementState;
        public WeaponState RequestedWeaponState;

        public readonly void Dispose()
        { }
    }
}
