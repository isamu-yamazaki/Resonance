using PurrNet;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    public class SonarDiscAbility : NetworkBehaviour, IAugmentAbility
    {
        [Header("References")]
        [SerializeField] private AugmentProperties augmentProperties;
        [SerializeField] private GameObject sonarDiscPrefab;

        [Header("Cooldown")]
        [SerializeField] private float cooldown = 12f;
        private float _cooldownTimeRemaining;

        private PlayerActionsInput _playerActionsInput;
        private FPArmsManager _fpArmsManager;

        public const string AbilityKeyConst = "augment_upper_sonarDisc";

        public string AbilityKey => AbilityKeyConst;
        public string Name => "Sonar Disc";
        public string Description => augmentProperties != null ? augmentProperties.Description : string.Empty;
        public float MaxCooldown => cooldown;
        public float CurrentCooldown
        {
            get => _cooldownTimeRemaining;
            set => _cooldownTimeRemaining = Mathf.Clamp(value, 0f, cooldown);
        }
        public bool AbilityReady => _cooldownTimeRemaining <= 0f;

        #region Network

        protected override void OnSpawned()
        {
            base.OnSpawned();

            if (isOwner)
            {
                _playerActionsInput = PlayerActionsInput.Instance;
                _fpArmsManager = GetComponent<FPArmsManager>();
            }
        }

        #endregion

        private void Update()
        {
            if (!isOwner) return;

            if (_cooldownTimeRemaining > 0f)
                _cooldownTimeRemaining -= Time.deltaTime;
        }

        private Transform GetActiveMuzzle()
        {
            if (_fpArmsManager == null)
            {
                Debug.LogWarning("[SonarDiscAbility] No FPArmsManager found on this GameObject.");
                return null;
            }

            WeaponView view = _fpArmsManager.GetActiveFPWeaponView();
            if (view == null || view.Muzzle == null)
            {
                Debug.LogWarning("[SonarDiscAbility] No active FP weapon view or muzzle found.");
                return null;
            }

            return view.Muzzle;
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

            Transform muzzle = GetActiveMuzzle();
            if (muzzle == null) return;

            _cooldownTimeRemaining = cooldown;
            RequestFireDiscServerRpc(muzzle.position, muzzle.forward, NetworkManager.main.localPlayer);
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