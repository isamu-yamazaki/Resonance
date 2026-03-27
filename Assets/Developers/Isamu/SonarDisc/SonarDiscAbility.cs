using PurrNet;
using Resonance.Combat.Augments;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Ability script for the Sonar Disc.
    /// Instantiates and fires the disc projectile from the player camera.
    /// TODO: Replace camera reference with muzzle point transform on the player's left arm.
    /// </summary>
    public class SonarDiscAbility : NetworkBehaviour, IAugmentAbility
    {
        [Header("References")]
        [SerializeField] private AugmentProperties augmentProperties;
        [SerializeField] private GameObject sonarDiscPrefab;
        [SerializeField] private Camera playerCamera;

        [Header("Cooldown")]
        [SerializeField] private float cooldown = 12f;
        private float _cooldownTimeRemaining;

        private PlayerActionsInput _playerActionsInput;

        public const string AbilityKey = "augment_upper_sonarDisc";

        public string Name => AbilityKey;
        public string Description => augmentProperties != null ? augmentProperties.Description : string.Empty;

        public float Cooldown
        {
            get => cooldown;
            set => cooldown = value;
        }

        #region Network

        protected override void OnSpawned()
        {
            base.OnSpawned();
            enabled = isOwner;

            if (isOwner && playerCamera == null)
                playerCamera = Camera.main;

            if (isOwner)
                _playerActionsInput = GetComponent<PlayerActionsInput>();
        }

        #endregion

        private void Update()
        {
            if (_cooldownTimeRemaining > 0f)
                _cooldownTimeRemaining -= Time.deltaTime;

            if (_playerActionsInput != null && _playerActionsInput.AbilityUpperPressed)
            {
                _playerActionsInput.SetAbilityUpperPressedFalse();
                ActivateAbility();
            }
        }

        public void ActivateAbility()
        {
            if (_cooldownTimeRemaining > 0f)
                return;

            if (sonarDiscPrefab == null)
            {
                Debug.LogWarning("[SonarDiscAbility] sonarDiscPrefab is not assigned.");
                return;
            }

            if (playerCamera == null)
            {
                Debug.LogWarning("[SonarDiscAbility] playerCamera is not assigned.");
                return;
            }

            // TODO: Replace cameraTransform.position/forward with muzzlePoint.position/forward
            _cooldownTimeRemaining = cooldown;
            Transform cameraTransform = playerCamera.transform;
            RequestFireDiscServerRpc(cameraTransform.position, cameraTransform.forward, NetworkManager.main.localPlayer);
        }

        [ServerRpc]
        private void RequestFireDiscServerRpc(Vector3 spawnPosition, Vector3 direction, PlayerID firingPlayerID)
        {
            GameObject discInstance = Instantiate(sonarDiscPrefab, spawnPosition, Quaternion.LookRotation(direction));
            NetworkManager.main.Spawn(discInstance);

            SonarDiscProjectile disc = discInstance.GetComponent<SonarDiscProjectile>();
            if (disc == null)
            {
                Debug.LogError("[SonarDiscAbility] sonarDiscPrefab is missing a SonarDiscProjectile component.");
                return;
            }

            disc.Launch(direction, gameObject, firingPlayerID);
            disc.BroadcastShootSoundObserversRpc();
        }
    }
}
