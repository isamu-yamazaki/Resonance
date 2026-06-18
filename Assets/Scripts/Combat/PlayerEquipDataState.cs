using PurrNet.Prediction;

namespace Resonance.Combat
{
    public struct PlayerEquipDataState : IPredictedData<PlayerEquipDataState>
    {
        /// <summary>
        /// Maps to the type of weapon that is equipped.
        /// </summary>
        public string EquippedWeaponKey;

        /// <summary>
        /// Maps to a unique instance of the equipped weapon.
        /// </summary>
        public string EquippedWeaponId;

        public int CurrentSlot;
        public int LastSlot;

        public readonly void Dispose() { }
    }
}
