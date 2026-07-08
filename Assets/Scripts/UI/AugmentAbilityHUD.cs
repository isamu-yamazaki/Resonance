using System;
using System.Collections;
using Resonance.Helper;
using Resonance.PlayerController;
using UnityEngine.InputSystem;
using UnityEngine;

namespace Resonance.Combat.Augments.UI
{
    public class AugmentHUDManager : MonoBehaviour
    {
        public static AugmentHUDManager Instance { get; private set; }

        [SerializeField] private AugmentSlotUI upperSlotUI;
        [SerializeField] private AugmentSlotUI lowerSlotUI;

        private PlayerAbilityManager _playerAbilityManager;

        private void Awake()
        {
            Instance = this;


            var controls = PlayerController.PlayerInputManager.Instance.PlayerControls;
            upperSlotUI.SetKeybindLabel(controls.PlayerActionMap.AbilityUpper.GetBindingDisplayString());
            lowerSlotUI.SetKeybindLabel(controls.PlayerActionMap.AbilityLower.GetBindingDisplayString());
        }

        private void Start()
        {
            StartCoroutine(BindToPlayerAbilityManager());
        }

        private IEnumerator BindToPlayerAbilityManager()
        {
            while (OwnerFinder.FindFirstOwnedPredictedObjectByType<PlayerAbilityManager>() == null)
            {
                yield return null;
            }

            _playerAbilityManager = OwnerFinder.FindFirstOwnedPredictedObjectByType<PlayerAbilityManager>();

            SubscribeToPlayerAbilityManager();
        }

        private void SubscribeToPlayerAbilityManager()
        {
            if (_playerAbilityManager == null) return;

            _playerAbilityManager.OnAbilityUsed += OnAbilityUsed;
            _playerAbilityManager.OnAugmentEquipped += OnAugmentEquipped;
            _playerAbilityManager.OnAugmentRemoved += OnAugmentRemoved;
        }

        private void OnDestroy()
        {
            UnsubscribeFromPlayerAbilityManager();
        }

        private void UnsubscribeFromPlayerAbilityManager()
        {
            if (_playerAbilityManager == null) return;

            _playerAbilityManager.OnAbilityUsed -= OnAbilityUsed;
            _playerAbilityManager.OnAugmentEquipped -= OnAugmentEquipped;
            _playerAbilityManager.OnAugmentRemoved -= OnAugmentRemoved;
        }

        public void OnAugmentEquipped(AugmentProperties augment, IAugmentAbility ability)
        {
            var ui = augment.Slot == AugmentSlot.Upper ? upperSlotUI : lowerSlotUI;
            ui.SetAugment(augment, ability);
        }

        public void OnAugmentRemoved(AugmentSlot slot)
        {
            var ui = slot == AugmentSlot.Upper ? upperSlotUI : lowerSlotUI;
            ui.ClearAugment();
        }

        public void OnAbilityUsed(AugmentSlot slot)
        {
            var ui = slot == AugmentSlot.Upper ? upperSlotUI : lowerSlotUI;
            ui.OnAbilityActivated();
        }
    }
}