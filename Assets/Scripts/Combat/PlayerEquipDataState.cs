using PurrNet.Prediction;
using Resonance.Combat.Weapons;

namespace Resonance.Combat
{
    public struct PlayerEquipDataState : IPredictedData<PlayerEquipDataState>
    {
        /// <summary>
        /// Weapon identity equipped on the last tick.
        /// Currently equipped weapon info comes from player inventory.
        /// </summary>
        public WeaponIdentity? LastEquippedWeapon;

        public int CurrentSlot;
        public int LastSlot;

        public readonly void Dispose() { }
    }
}
