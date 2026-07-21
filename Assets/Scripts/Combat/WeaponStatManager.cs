using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PurrNet.Pooling;
using PurrNet.Prediction;
using Resonance.Combat.Mods;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;

namespace Resonance.Combat
{
    public class WeaponStatManager : PredictedIdentity<WeaponStatManagerInput, WeaponStatManagerState>
    {
        private WeaponModProperties[] _mods;

        // Pending augment-mod operations queued by the non-predicted mutator API
        // (PlayerAugmentEquipper, AbilityHumanTurret); drained into the input in GetFinalInput.
        private string _pendingAugmentAdd;
        private string _pendingAugmentRemove;
        private bool _pendingClear;

        // Pending weapon-to-manage change queued by non-simulation callers (e.g. the shop via
        // PlayerEquip.RemoveWeapon); drained into the input in GetFinalInput. The flag distinguishes
        // "no change this tick" from "set to null" (unequip).
        private WeaponIdentity? _pendingWeaponToManage;
        private bool _hasPendingWeaponManage;

        [CanBeNull]
        public WeaponProperties ManagedWeapon
        {
            get => WeaponResolver.ResolveWeapon(currentState.WeaponIdentity);
        }

        #region Lifecycle

        protected override void LateAwake()
        {
            _mods = Resources.LoadAll<WeaponModProperties>("Content/Mods");

            // Augment-mod changes are one-shot keys carried in the input; repeating (extrapolating)
            // the last input on non-owner clients would re-add the same key every tick. Non-owners
            // receive the authoritative AugmentModKeys via state sync, so disabling extrapolation is safe.
            _extrapolateInput = false;
        }

        protected override WeaponStatManagerState GetInitialState()
        {
            return new WeaponStatManagerState()
            {
                AugmentModKeys = DisposableList<string>.Create(),
                WeaponIdentity = null
            };
        }

        #endregion

        #region Input

        protected override void GetFinalInput(ref WeaponStatManagerInput input)
        {
            if (_pendingClear)
            {
                input.ClearAugmentMods = true;
                _pendingClear = false;
            }

            if (_pendingAugmentAdd != null)
            {
                input.AugmentKeyToAdd = _pendingAugmentAdd;
                _pendingAugmentAdd = null;
            }

            if (_pendingAugmentRemove != null)
            {
                input.AugmentKeyToRemove = _pendingAugmentRemove;
                _pendingAugmentRemove = null;
            }

            if (_hasPendingWeaponManage)
            {
                input.ManageWeapon = true;
                input.WeaponToManage = _pendingWeaponToManage;
                _hasPendingWeaponManage = false;
                _pendingWeaponToManage = null;
            }
        }

        // Queues a weapon-to-manage change from non-simulation code. Pass null to clear (unequip).
        public void SetWeaponToManageExternal(WeaponProperties weaponToManage)
        {
            _pendingWeaponToManage = weaponToManage != null
                ? WeaponIdentity.FromWeaponProperties(weaponToManage)
                : null;
            _hasPendingWeaponManage = true;
        }

        public void AddAugmentMod(WeaponModProperties mod)
        {
            if (mod == null) return;
            _pendingAugmentAdd = mod.Key;
        }

        public void RemoveAugmentMod(WeaponModProperties mod)
        {
            if (mod == null) return;
            _pendingAugmentRemove = mod.Key;
        }

        public void ClearAugmentMods()
        {
            _pendingClear = true;
        }

        #endregion

        #region Simulation

        [SimulationOnly]
        public void SetWeaponPropertiesToManage(WeaponProperties weaponToManage)
        {
            var identity = WeaponIdentity.FromWeaponProperties(weaponToManage);
            SetWeaponToManage(identity);
        }

        [SimulationOnly]
        public void SetWeaponToManage(WeaponIdentity? identity)
        {
            currentState.WeaponIdentity = identity;
        }

        [SimulationOnly]
        public void SimulateAddAugmentMod(WeaponModProperties mod)
        {
            if (mod == null) return;
            SimulateAddAugmentMod(ref currentState, mod.Key);
        }

        private void SimulateAddAugmentMod(ref WeaponStatManagerState state, string key)
        {
            state.AugmentModKeys.Add(key);
        }

        [SimulationOnly]
        public void SimulateRemoveAugmentMod(WeaponModProperties mod)
        {
            if (mod == null) return;
            SimulateRemoveAugmentMod(ref currentState, mod.Key);
        }

        private void SimulateRemoveAugmentMod(ref WeaponStatManagerState state, string key)
        {
            // Remove the first matching key so multiplicity (e.g. two augments sharing a mod) is preserved.
            state.AugmentModKeys.Remove(key);
        }

        [SimulationOnly]
        public void SimulateClearAugmentMods()
        {
            SimulateClearAugmentMods(ref currentState);
        }

        private void SimulateClearAugmentMods(ref WeaponStatManagerState state)
        {
            state.AugmentModKeys.Clear();
        }

        protected override void Simulate(WeaponStatManagerInput input, ref WeaponStatManagerState state, float delta)
        {
            if (input.ManageWeapon)
                state.WeaponIdentity = input.WeaponToManage;

            if (input.ClearAugmentMods)
                SimulateClearAugmentMods(ref state);

            if (input.AugmentKeyToRemove != null)
                SimulateRemoveAugmentMod(ref state, input.AugmentKeyToRemove);

            if (input.AugmentKeyToAdd != null)
                SimulateAddAugmentMod(ref state, input.AugmentKeyToAdd);
        }

        #endregion

        #region State getters

        // Resolves the predicted augment-mod keys back to their WeaponModProperties assets.
        private IEnumerable<WeaponModProperties> ResolveAugmentMods()
        {
            foreach (var key in currentState.AugmentModKeys)
            {
                var mod = Array.Find(_mods, m => m.Key == key);
                if (mod != null) yield return mod;
            }
        }

        public float GetStat(WeaponStat stat)
        {
            var managedWeapon = ManagedWeapon;
            if (managedWeapon == null) return 0f;

            float baseStat = GetBaseValue(stat);
            float additiveSum = 0f;
            float multiplicativeProduct = 1f;

            IEnumerable<WeaponModProperties> allMods =
                managedWeapon.ModList.Concat(ResolveAugmentMods()).Where(mod => mod != null);

            foreach (WeaponModProperties mod in allMods)
            {
                foreach (StatModifier modifier in mod.Modifiers)
                {
                    if (modifier.stat != stat) continue;

                    if (modifier.type == ModifierType.Additive)
                        additiveSum += modifier.value;
                    else
                        multiplicativeProduct *= modifier.value;
                }
            }

            return (baseStat + additiveSum) * multiplicativeProduct;
        }

        public BulletProperties GetBulletProperties()
        {
            var managedWeapon = ManagedWeapon;
            if (managedWeapon == null) return null;

            IEnumerable<WeaponModProperties> allMods =
                managedWeapon.ModList.Concat(ResolveAugmentMods()).Where(mod => mod != null);

            foreach (WeaponModProperties mod in allMods)
            {
                if (mod.BulletPropertiesOverride != null)
                    return mod.BulletPropertiesOverride;
            }

            return managedWeapon.BulletProperties;
        }

        public MuzzleFlashSettings GetMuzzleFlashSettings()
        {
            var managedWeapon = ManagedWeapon;
            if (managedWeapon == null) return null;

            IEnumerable<WeaponModProperties> allMods =
                managedWeapon.ModList.Concat(ResolveAugmentMods()).Where(mod => mod != null);

            foreach (WeaponModProperties mod in allMods)
            {
                if (mod.Slot == ModSlot.Barrel && mod.MuzzleFlashOverride != null)
                    return mod.MuzzleFlashOverride;
            }

            return null;
        }

        // Returns barrel mod audio override if one exists, falls back to base weapon audio.
        public WeaponAudioProperties GetAudioProperties()
        {
            var managedWeapon = ManagedWeapon;
            if (managedWeapon == null) return null;

            IEnumerable<WeaponModProperties> allMods =
                managedWeapon.ModList.Concat(ResolveAugmentMods()).Where(mod => mod != null);

            foreach (WeaponModProperties mod in allMods)
            {
                if (mod.Slot == ModSlot.Barrel && mod.AudioOverride != null)
                    return mod.AudioOverride;
            }

            return managedWeapon.AudioProperties;
        }

        private float GetBaseValue(WeaponStat stat)
        {
            var managedWeapon = ManagedWeapon;
            if (managedWeapon != null)
            {
                return stat switch
                {
                    WeaponStat.Damage => managedWeapon.Damage,
                    WeaponStat.FireRate => managedWeapon.FireRate,
                    WeaponStat.ProjectilesPerShot => managedWeapon.ProjectilesPerShot,
                    WeaponStat.Range => managedWeapon.Range,
                    WeaponStat.Accuracy => managedWeapon.Accuracy,
                    WeaponStat.Control => managedWeapon.Control,
                    WeaponStat.Spread => managedWeapon.Spread,
                    WeaponStat.MuzzleVelocity => managedWeapon.MuzzleVelocity,
                    WeaponStat.Mobility => managedWeapon.Mobility,
                    WeaponStat.Handling => managedWeapon.Handling,
                    WeaponStat.MagazineSize => managedWeapon.MagazineSize,
                    WeaponStat.ReloadTime => managedWeapon.ReloadTime,
                    WeaponStat.SpreadPerShot => managedWeapon.SpreadPerShot,
                    WeaponStat.MaxSpread => managedWeapon.MaxSpread,
                    WeaponStat.SpreadRecoveryRate => managedWeapon.SpreadRecoveryRate,
                    _ => 0f
                };
            }

            return 0f;
        }

        #endregion


        public float Damage => GetStat(WeaponStat.Damage);
        public float FireRate => GetStat(WeaponStat.FireRate);
        public int ProjectilesPerShot => Mathf.RoundToInt(GetStat(WeaponStat.ProjectilesPerShot));
        public float Range => GetStat(WeaponStat.Range);
        public float Accuracy => GetStat(WeaponStat.Accuracy);
        public float Control => GetStat(WeaponStat.Control);
        public float Spread => GetStat(WeaponStat.Spread);
        public float MuzzleVelocity => GetStat(WeaponStat.MuzzleVelocity);
        public float Mobility => GetStat(WeaponStat.Mobility);
        public float Handling => GetStat(WeaponStat.Handling);
        public int MagazineSize => Mathf.RoundToInt(GetStat(WeaponStat.MagazineSize));
        public float ReloadTime => GetStat(WeaponStat.ReloadTime);
        public float SpreadPerShot => GetStat(WeaponStat.SpreadPerShot);
        public float MaxSpread => GetStat(WeaponStat.MaxSpread);
        public float SpreadRecoveryRate => GetStat(WeaponStat.SpreadRecoveryRate);
    }

    public struct WeaponStatManagerInput : IPredictedData
    {
        public string AugmentKeyToAdd;
        public string AugmentKeyToRemove;
        public bool ClearAugmentMods;

        public WeaponIdentity? WeaponToManage;
        public bool ManageWeapon;

        public void Dispose()
        {
        }
    }

    public struct WeaponStatManagerState : IPredictedData<WeaponStatManagerState>
    {
        public WeaponIdentity? WeaponIdentity;
        public DisposableList<string> AugmentModKeys;

        public void Dispose()
        {
            AugmentModKeys.Dispose();
        }
    }
}