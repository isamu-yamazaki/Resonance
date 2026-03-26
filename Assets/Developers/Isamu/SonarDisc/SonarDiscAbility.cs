using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Ability script for the Sonar Disc.
    /// Instantiates and fires the disc projectile from the player camera.
    /// TODO: Implement IAbility interface once provided.
    /// TODO: Remove Update() T keybind and call Use() from IAbility.Use() instead.
    /// </summary>
    public class SonarDiscAbility : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject sonarDiscPrefab;

        // TODO: Replace camera reference with muzzle point transform on the player's left arm
        [SerializeField] private Camera playerCamera;

        [Header("Cooldown")]
        [SerializeField] private float cooldown = 12f;

        private float _cooldownTimeRemaining;

        private void Awake()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        // TODO: Replace with IAbility implementation
        public bool IsOnCooldown => _cooldownTimeRemaining > 0f;

        private void Update()
        {
            if (_cooldownTimeRemaining > 0f)
                _cooldownTimeRemaining -= Time.deltaTime;

            // TODO: Remove this keybind — temporary test input only
            if (Keyboard.current.tKey.wasPressedThisFrame)
                Use();
        }

        // TODO: Call this from IAbility.Use() once interface is provided
        public void Use()
        {
            if (IsOnCooldown)
                return;

            FireDisc();
            _cooldownTimeRemaining = cooldown;
        }

        private void FireDisc()
        {
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
            Transform cameraTransform = playerCamera.transform;
            GameObject discInstance = Instantiate(sonarDiscPrefab, cameraTransform.position, cameraTransform.rotation);
            SonarDiscProjectile disc = discInstance.GetComponent<SonarDiscProjectile>();

            if (disc == null)
            {
                Debug.LogError("[SonarDiscAbility] sonarDiscPrefab is missing a SonarDiscProjectile component.");
                return;
            }

            disc.Launch(cameraTransform.forward, Resonance.PlayerController.PlayerController.LocalPlayer.gameObject);
        }
    }
}
