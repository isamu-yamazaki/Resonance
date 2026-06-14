using PurrNet;
using PurrNet.Prediction;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    public class SonarDiscAbility : PredictedIdentity<SonarDiscAbilityInput, SonarDiscAbilityState>, IAugmentAbility
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

        protected override void LateAwake()
        {
            if (!isOwner) return;

            _playerActionsInput = PlayerActionsInput.Instance;
            _fpArmsManager = GetComponent<FPArmsManager>();
        }

        #endregion

        private void Update()
        {
            if (!isOwner) return;

            if (_cooldownTimeRemaining > 0f)
                _cooldownTimeRemaining -= Time.deltaTime;
        }

        protected override void Simulate(SonarDiscAbilityInput input, ref SonarDiscAbilityState state, float delta)
        {
            throw new System.NotImplementedException();
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

        public void ActivateAbilityExternal()
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

        public void SimulateActivateAbility()
        {
            throw new System.NotImplementedException();
        }

        private void RequestFireDiscServerRpc(Vector3 spawnPosition, Vector3 direction, PlayerID firingPlayerID)
        {
            // TODO: assign owner when creating in hierarchy

            GameObject discInstance = Instantiate(sonarDiscPrefab, spawnPosition, Quaternion.LookRotation(direction));
            NetworkManager.main.Spawn(discInstance);

            SonarDiscProjectile disc = discInstance.GetComponent<SonarDiscProjectile>();
            if (disc == null)
            {
                Debug.LogError("[SonarDiscAbility] sonarDiscPrefab is missing a SonarDiscProjectile component.");
                return;
            }

            disc.Launch(direction);
            disc.BroadcastShootSound();
        }
    }

    public struct SonarDiscAbilityState : IPredictedData<SonarDiscAbilityState>
    {
        public void Dispose()
        {
        }
    }

    public struct SonarDiscAbilityInput : IPredictedData
    {
        public void Dispose()
        {
        }
    }
}
