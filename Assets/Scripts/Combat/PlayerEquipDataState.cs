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

        /// <summary>
        /// Augment keys equipped on the last tick, per slot. Currently equipped augment
        /// info comes from player inventory; these snapshots let the simulation detect
        /// equip/swap/remove edges and orchestrate side effects deterministically.
        /// </summary>
        public string LastEquippedUpperAugmentKey;
        public string LastEquippedLowerAugmentKey;

        public int CurrentSlot;
        public int LastSlot;

        public readonly void Dispose() { }
    }
}
