using System;
using Resonance.Combat.Weapons.Enums;

namespace Resonance.Combat.Mods
{
    [Serializable]
    public class StatModifier
    {
        public WeaponStat stat;
        public ModifierType type;
        public float value;
    }
}
