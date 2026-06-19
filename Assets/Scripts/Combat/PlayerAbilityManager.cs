using System.Collections.Generic;
using PurrNet.Packing;
using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Augments.UI;
using Resonance.Inventory;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat
{
    public class PlayerAbilityManager : PredictedIdentity<PlayerAbilityManagerInput, PlayerAbilityManagerState>
    {
        #region Fields

        private PlayerActionsInput playerActionsInput;
        private PlayerInventory inventory;
        private FPArmsAnimator fpArmsAnimator;
        private Dictionary<string, IAugmentAbility> abilityMap = new();

        private AugmentProperties[] _augmentResources;

        private AugmentLookupArguments? _pendingAugmentToEquip;
        private AugmentLookupArguments? _pendingAugmentToRemove;

        #endregion

        #region Startup

        protected override void LateAwake()
        {
            _augmentResources = Resources.LoadAll<AugmentProperties>("Content/Augments");
            inventory = GetComponent<PlayerInventory>();
            fpArmsAnimator = GetComponent<FPArmsAnimator>();

            foreach (IAugmentAbility ability in GetComponents<IAugmentAbility>())
            {
                abilityMap[ability.AbilityKey] = ability;

                // Abilities stay permanently enabled; equipped-ness is gated through predicted state
                // (IEquippableAbility), not the Unity `enabled` flag. Force enabled so a stale prefab
                // value can't suppress input/simulation. They start unequipped via their own state.
                if (ability is MonoBehaviour mb)
                    mb.enabled = true;
            }

            if (!isOwner) return;
            playerActionsInput = PlayerActionsInput.Instance;
        }

        #endregion

        #region External state-setters

        public void OnAugmentEquipped(AugmentProperties augment)
        {
            if (augment == null || string.IsNullOrEmpty(augment.AbilityKey))
            {
                return;
            }

            _pendingAugmentToEquip = new AugmentLookupArguments
            {
                AbilityKey = augment.AbilityKey,
                Key = augment.Key
            };
        }

        public void OnAugmentRemoved(AugmentProperties augment)
        {
            if (augment == null || string.IsNullOrEmpty(augment.AbilityKey))
            {
                return;
            }

            _pendingAugmentToRemove = new AugmentLookupArguments()
            {
                AbilityKey = augment.AbilityKey,
                Key = augment.Key
            };
        }

        #endregion

        #region Simulation loop

        protected override void GetFinalInput(ref PlayerAbilityManagerInput input)
        {
            if (!isOwner) return;
            input.AbilityUpperPressed = playerActionsInput.AbilityUpperPressed;
            input.AbilityLowerPressed =  playerActionsInput.AbilityLowerPressed;

            input.PendingAugmentToEquip = _pendingAugmentToEquip;
            input.PendingAugmentToRemove = _pendingAugmentToRemove;

            _pendingAugmentToEquip = null;
            _pendingAugmentToRemove = null;
        }

        protected override void Simulate(PlayerAbilityManagerInput input, ref PlayerAbilityManagerState state, float delta)
        {
            if (input.AbilityUpperPressed)
            {
                TryUseUpperActiveAbility();
            }

            if (input.AbilityLowerPressed)
            {
                TryUseLowerActiveAbility();
            }

            if (input.PendingAugmentToEquip != null)
            {
                TryEquipAugment(input.PendingAugmentToEquip.Value.AbilityKey);
            }
            state.AugmentEquippedThisTick = input.PendingAugmentToEquip;

            if (input.PendingAugmentToRemove != null)
            {
                TryRemoveAugment(input.PendingAugmentToRemove.Value.AbilityKey);
            }
            state.AugmentRemovedThisTick = input.PendingAugmentToRemove;
        }

        [SimulationOnly]
        private void TryRemoveAugment(string abilityKey)
        {
            if (abilityMap.TryGetValue(abilityKey, out IAugmentAbility ability))
            {
                SetAbilityEquipped(ability, false);
            }
        }

        [SimulationOnly]
        private void TryEquipAugment(string abilityKey)
        {
            if (abilityMap.TryGetValue(abilityKey, out IAugmentAbility ability))
            {
                SetAbilityEquipped(ability, true);
            }
        }


        [SimulationOnly]
        private void TryUseUpperActiveAbility()
        {
            IAugmentAbility ability = GetAbility(inventory.AugmentInventory[0]?.AbilityKey);
            if (ability == null) return;
            if (!ability.AbilityReady) return;

            // if (ability is AbilityGrappleHook)
            // {
            //     fpArmsAnimator?.RequestGrappleActivation();
            //     return;
            // }

            ability.SimulateActivateAbility();
        }

        [SimulationOnly]
        private void TryUseLowerActiveAbility()
        {
            IAugmentAbility ability = GetAbility(inventory.AugmentInventory[1]?.AbilityKey);
            if (ability == null || !ability.AbilityReady) return;

            // if (ability is AbilityGrappleHook)
            // {
            //     fpArmsAnimator?.RequestGrappleActivation();
            //     return;
            // }

            ability.SimulateActivateAbility();
        }

        [SimulationOnly]
        private void SetAbilityEquipped(IAugmentAbility ability, bool equipped)
        {
            // Equippable abilities gate themselves through predicted state (or a plain flag for the
            // non-predicted turret) and stay permanently enabled. Everything else is left enabled and
            // remains gated by the inventory/activation flow — we never toggle `mb.enabled` here, as
            // disabling a PredictedIdentity breaks its input transmission to the server.
            if (ability is IEquippableAbility equippable)
                equippable.SetEquipped(equipped);
        }

        #endregion

        #region Side effects

        protected override void UpdateView(PlayerAbilityManagerState viewState, PlayerAbilityManagerState? verified)
        {
            // use verified state only, for now
            if (!verified.HasValue) return;
            var v = verified.Value;

            if (v.UpperAbilityFired)
            {
                AugmentHUDManager.Instance.OnAbilityUsed(AugmentSlot.Upper);
            }

            if (v.LowerAbilityFired)
            {
                AugmentHUDManager.Instance.OnAbilityUsed(AugmentSlot.Lower);
            }

            if (v.AugmentEquippedThisTick.HasValue)
            {
                var lookupArgs = v.AugmentEquippedThisTick.Value;
                var ability = GetAbility(lookupArgs.AbilityKey);
                var augment = FindAugmentByKey(lookupArgs.Key);
                if (augment != null && ability != null)
                {
                    AugmentHUDManager.Instance.OnAugmentEquipped(augment, ability);
                }
            }

            if (v.AugmentRemovedThisTick.HasValue)
            {
                var lookupArgs = v.AugmentRemovedThisTick.Value;
                var augment = FindAugmentByKey(lookupArgs.Key);
                if (augment != null)
                {
                    AugmentHUDManager.Instance.OnAugmentRemoved(augment.Slot);
                }
            }
        }
        #endregion

        #region Helpers
        private IAugmentAbility GetAbility(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            abilityMap.TryGetValue(key, out IAugmentAbility ability);
            return ability;
        }

        private AugmentProperties FindAugmentByKey(string key)
        {
            if (string.IsNullOrEmpty(key) || _augmentResources == null) return null;
            return System.Array.Find(_augmentResources, a => a.Key == key);
        }

        #endregion
    }

    public struct PlayerAbilityManagerState : IPredictedData<PlayerAbilityManagerState>
    {
        public bool UpperAbilityFired;
        public bool LowerAbilityFired;

        public AugmentLookupArguments? AugmentEquippedThisTick;
        public AugmentLookupArguments? AugmentRemovedThisTick;

        public void Dispose()
        {
        }
    }

    public struct PlayerAbilityManagerInput : IPredictedData
    {
        public bool AbilityUpperPressed;
        public bool AbilityLowerPressed;
        public AugmentLookupArguments? PendingAugmentToEquip;
        public AugmentLookupArguments? PendingAugmentToRemove;

        public void Dispose()
        {
        }
    }

    public struct AugmentLookupArguments : IPackedAuto
    {
        public string Key;
        public string AbilityKey;
    }
}
