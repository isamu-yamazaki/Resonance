using PurrNet;
using Resonance.Combat.Augments;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Ability script for the Sonar Disc.
    /// Instantiates and fires the disc projectile from the player camera.
    /// TODO: Implement IAbility interface once provided.
    /// TODO: Remove Update() T keybind and call ActivateAbility() from input system instead.
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

        public const string AbilityKey = "augment_upper_sonarDisc";

        public string Name => AbilityKey;
        public string Description => augmentProperties != null ? augmentProperties.Description : string.Empty;
        public bool IsOnCooldown => _cooldownTimeRemaining > 0f;

        #region Network

        protected override void OnSpawned()
        {
            base.OnSpawned();
            enabled = isOwner;

            if (isOwner && playerCamera == null)
                playerCamera = Camera.main;
        }

        #endregion

        private void Update()
        {
            if (_cooldownTimeRemaining > 0f)
                _cooldownTimeRemaining -= Time.deltaTime;

            // TODO: Remove this keybind — temporary test input only
            if (Keyboard.current.tKey.wasPressedThisFrame)
                ActivateAbility();
        }

        public void ActivateAbility()
        {
            if (IsOnCooldown)
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

            _cooldownTimeRemaining = cooldown;

            // TODO: Replace cameraTransform.position/forward with muzzlePoint.position/forward
            Transform cameraTransform = playerCamera.transform;
            RequestFireDiscServerRpc(cameraTransform.position, cameraTransform.forward);
        }

        [ServerRpc]
        private void RequestFireDiscServerRpc(Vector3 spawnPosition, Vector3 direction)
        {
            GameObject discInstance = Instantiate(sonarDiscPrefab, spawnPosition, Quaternion.LookRotation(direction));
            NetworkManager.main.Spawn(discInstance);

            SonarDiscProjectile disc = discInstance.GetComponent<SonarDiscProjectile>();
            if (disc == null)
            {
                Debug.LogError("[SonarDiscAbility] sonarDiscPrefab is missing a SonarDiscProjectile component.");
                return;
            }

            disc.Launch(direction, gameObject, NetworkManager.main.localPlayer);
        }
    }
}
