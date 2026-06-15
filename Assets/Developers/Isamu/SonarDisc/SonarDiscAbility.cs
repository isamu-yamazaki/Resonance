using PurrNet;
using PurrNet.Prediction;
using Resonance.Assemblies.AbilitySimulation.SonarDisc;
using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    public class SonarDiscAbility : PredictedIdentity<SonarDiscAbilityInput, SonarDiscAbilityState>, IAugmentAbility
    {
        [Header("References")] [SerializeField]
        private AugmentProperties augmentProperties;

        [SerializeField] private GameObject sonarDiscPrefab;

        [Header("Config")] [SerializeField] private SonarDiscConfig config;

        private PlayerActionsInput _playerActionsInput;
        private FPArmsManager _fpArmsManager;
        private bool _pendingActivate;

        public const string AbilityKeyConst = "augment_upper_sonarDisc";

        public string AbilityKey => AbilityKeyConst;
        public string Name => "Sonar Disc";
        public string Description => augmentProperties != null ? augmentProperties.Description : string.Empty;
        public float MaxCooldown => config.cooldown;
        public float CurrentCooldown => currentState.Cooldown;
        public bool AbilityReady => CurrentCooldown <= 0f;

        #region Setup

        protected override SonarDiscAbilityState GetInitialState()
        {
            return new SonarDiscAbilityState()
            {
                Cooldown = config.cooldown
            };
        }

        protected override void LateAwake()
        {
            if (!isOwner) return;

            _playerActionsInput = PlayerActionsInput.Instance;
            _fpArmsManager = GetComponent<FPArmsManager>();
        }

        #endregion

        #region Input

        public void ActivateAbilityExternal()
        {
            if (!AbilityReady)
                return;

            _pendingActivate = true;
        }

        private Transform GetActiveMuzzle()
        {
            // this part is client side
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

        protected override void GetFinalInput(ref SonarDiscAbilityInput input)
        {
            input.ActivatePressed = _pendingActivate;
            _pendingActivate = false;

            Transform muzzle = GetActiveMuzzle();
            if (muzzle == null) return;

            input.MuzzlePosition = muzzle.position;
            input.MuzzleForward = muzzle.forward;
        }

        #endregion

        #region Simulation

        [SimulationOnly]
        public void SimulateActivateAbility()
        {
            currentState.SpawnDiscNextTick = true;
        }

        protected override void Simulate(SonarDiscAbilityInput input, ref SonarDiscAbilityState state, float delta)
        {
            var ctx = new SonarDiscSimulationContext(input, config, delta);
            SonarDiscSimulation.Step(ctx, ref state);

            if (state.ShouldSpawnDisc)
                FireDisc(state.SpawnPosition, state.SpawnDirection);
        }

        [SimulationOnly]
        private void FireDisc(Vector3 spawnPosition, Vector3 direction)
        {
            if (sonarDiscPrefab == null)
            {
                Debug.LogWarning("[SonarDiscAbility] sonarDiscPrefab is not assigned.");
                return;
            }

            PredictedObjectID? predictedObjectId =
                hierarchy.Create(sonarDiscPrefab, spawnPosition, Quaternion.identity, owner);
            GameObject instance = hierarchy.GetGameObject(predictedObjectId);

            SonarDiscProjectile projectile = instance.GetComponent<SonarDiscProjectile>();
            if (projectile == null)
            {
                Debug.LogError("[SonarDiscAbility] sonarDiscPrefab is missing a SonarDiscProjectile component.");
                return;
            }

            projectile.Launch(direction);
        }

        #endregion

    }
}