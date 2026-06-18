using System;
using PurrNet.Packing;

namespace Resonance.Combat.Weapons
{
    /// <summary>
    /// Contains all necessary properties needed to obtain a weapon
    /// type with a specific ID.
    /// </summary>
    [Serializable]
    public struct WeaponIdentity : IPackedAuto, IEquatable<WeaponIdentity>
    {
        public string Key;
        public string Id;

        public static WeaponIdentity? FromWeaponProperties(WeaponProperties weapon)
        {
            if (string.IsNullOrEmpty(weapon.Id) || string.IsNullOrEmpty(weapon.Key))
            {
                return null;
            }

            return new WeaponIdentity()
            {
                Id = weapon.Id,
                Key = weapon.Key
            };
        }

        public bool Equals(WeaponIdentity other)
        {
            return Key == other.Key && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is WeaponIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Id);
        }

        public static bool operator ==(WeaponIdentity a, WeaponIdentity b) => a.Equals(b);
        public static bool operator !=(WeaponIdentity a, WeaponIdentity b) => !a.Equals(b);
    }
}