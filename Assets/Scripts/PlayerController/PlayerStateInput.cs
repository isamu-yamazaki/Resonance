using PurrNet.Prediction;
using Resonance.Assemblies.Player;
using Resonance.Combat.Weapons.Enums;

namespace Resonance.PlayerController
{
    public struct PlayerStateInput : IPredictedData
    {
        public bool RequestExternalPlayerMovementStateUpdate;
        public bool RequestExternalWeaponStateUpdate;
        public bool RequestExternalWeaponClassUpdate;

        public PlayerMovementState RequestedPlayerMovementState;
        public WeaponState RequestedWeaponState;
        public WeaponClass RequestedWeaponClass;

        public readonly void Dispose()
        { }
    }
}
