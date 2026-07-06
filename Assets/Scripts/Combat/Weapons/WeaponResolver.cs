using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Resonance.Combat.Weapons
{
    /// <summary>
    /// Resolves <see cref="WeaponProperties"/> assets from the network-safe
    /// <see cref="WeaponIdentity"/>. This is the inverse of
    /// <see cref="WeaponIdentity.FromWeaponProperties"/>: an identity carries a
    /// <see cref="WeaponProperties.Key"/> that names the base asset plus a runtime
    /// <see cref="WeaponProperties.Id"/> that the resolved clone is stamped with.
    /// </summary>
    public static class WeaponResolver
    {
        private const string WeaponsResourcePath = "Content/Weapons";

        // Base weapon assets keyed by Key, populated once per play session.
        private static Dictionary<string, WeaponProperties> _baseWeaponsByKey;

        // Runs at the start of every play session (even when Domain Reload is
        // disabled), so a stale cache never survives from a previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCacheForNewPlaySession()
        {
            _baseWeaponsByKey = null;
        }

        private static Dictionary<string, WeaponProperties> BaseWeaponsByKey
        {
            get
            {
                if (_baseWeaponsByKey != null) return _baseWeaponsByKey;

                _baseWeaponsByKey = new Dictionary<string, WeaponProperties>();
                foreach (var weapon in Resources.LoadAll<WeaponProperties>(WeaponsResourcePath))
                {
                    if (weapon != null && !string.IsNullOrEmpty(weapon.Key))
                        _baseWeaponsByKey[weapon.Key] = weapon;
                }
                return _baseWeaponsByKey;
            }
        }

        /// <summary>
        /// Finds the raw base weapon asset with the given key. Returns null if the
        /// key is empty or no asset matches.
        /// </summary>
        [CanBeNull]
        public static WeaponProperties FindBaseWeaponByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return BaseWeaponsByKey.TryGetValue(key, out var weapon) ? weapon : null;
        }

        /// <summary>
        /// Resolves a network weapon identity into a fresh weapon instance stamped
        /// with the identity's runtime Id. Returns null if the identity's key is
        /// unknown.
        /// </summary>
        [CanBeNull]
        public static WeaponProperties ResolveWeapon(WeaponIdentity identity)
        {
            var baseWeapon = FindBaseWeaponByKey(identity.Key);
            return baseWeapon != null ? baseWeapon.Clone(identity.Id) : null;
        }

        /// <summary>
        /// Convenience overload for the many callers that hold a nullable identity.
        /// Returns null when the identity has no value.
        /// </summary>
        [CanBeNull]
        public static WeaponProperties ResolveWeapon(WeaponIdentity? identity)
            => identity.HasValue ? ResolveWeapon(identity.Value) : null;
    }
}
