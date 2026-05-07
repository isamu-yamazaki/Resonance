using PurrNet.Prediction;
using Resonance.Assemblies.Player;
using Resonance.Combat.Weapons.Enums;

namespace Resonance.PlayerController
{
    public struct PlayerStateData : IPredictedData<PlayerStateData>
    {
        public PlayerMovementState MovementState;
        public WeaponState WeaponState;
        public WeaponClass WeaponClass;
        public bool WeaponClassInitialized;

        public readonly void Dispose()
        {
        }
    }
}
