using PurrNet;
using PurrNet.Prediction;
using Resonance.Assemblies.AbilitySimulation.BubbleShield;
using Resonance.Combat.Augments;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.BubbleShield
{
    public class AbilityBubbleShield : PredictedIdentity<AbilityBubbleShieldInput, AbilityBubbleShieldState>,
        IAugmentAbility
    {
        [Header("Config")] [SerializeField] private BubbleShieldConfig config;

        [Header("References")] [SerializeField]
        private GameObject bubbleShieldPrefab;

#if !UNITY_SERVER
        [Header("Audio")] [SerializeField] private AK.Wwise.Event throwSoundEvent;
#endif

        private Camera playerCamera;
        private bool _pendingActivate;
        private AbilityBubbleShieldState? _previousVerifiedState;
        private float currentCooldown => currentState.Cooldown;

        public string AbilityKey => "ability_bubbleShield";
        public string Name => "Bubble Shield";
        public string Description => "Throw a shield that blocks bullets.";
        public float MaxCooldown => config.cooldown;

        public float CurrentCooldown => currentCooldown;

        public bool AbilityReady => currentCooldown <= 0f;

        public void ActivateAbilityExternal()
        {
            if (!AbilityReady) return;

            _pendingActivate = true;
        }

        [SimulationOnly]
        public void SimulateActivateAbility()
        {
            if (currentState.Cooldown <= 0)
            {
                currentState.Cooldown = config.cooldown;

                // both args determined from player's local input (for now)
                SpawnShield(currentState.SpawnPosition, currentState.LobDirection);
            }
        }

        protected override void LateAwake()
        {
            if (isOwner)
                playerCamera = Camera.main;
        }

        protected override void GetFinalInput(ref AbilityBubbleShieldInput input)
        {
            if (!isOwner) return;

            input.ActivatePressed = _pendingActivate;

            if (playerCamera != null)
            {
                input.SpawnPosition = playerCamera.transform.position;
                input.LobDirection = Vector3.Lerp(playerCamera.transform.forward, Vector3.up, config.upwardLobBias)
                    .normalized;
            }

            _pendingActivate = false;
        }


        protected override void Simulate(AbilityBubbleShieldInput input, ref AbilityBubbleShieldState state,
            float delta)
        {
            state.SpawnPosition = input.SpawnPosition;
            state.LobDirection = input.LobDirection;

            if (input.ActivatePressed)
            {
                state.Cooldown = config.cooldown;
                SpawnShield(input.SpawnPosition, input.LobDirection);
            }

            if (state.Cooldown > 0f)
                state.Cooldown -= delta;
        }

        protected override void UpdateView(AbilityBubbleShieldState viewState, AbilityBubbleShieldState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            var previousCooldown = _previousVerifiedState?.Cooldown ?? 0f;
            if (previousCooldown <= 0f && v.Cooldown > 0)
            {
#if !UNITY_SERVER
                throwSoundEvent?.Post(gameObject);
#endif
            }

            _previousVerifiedState = v;
        }

        [SimulationOnly]
        private void SpawnShield(Vector3 spawnPosition, Vector3 lobDirection)
        {
            if (bubbleShieldPrefab == null)
            {
                Debug.LogWarning("[AbilityBubbleShield] bubbleShieldPrefab is not assigned.");
                return;
            }

            PredictedObjectID? predictedObjectId =
                hierarchy.Create(bubbleShieldPrefab, spawnPosition, Quaternion.identity, owner);
            GameObject instance = hierarchy.GetGameObject(predictedObjectId);
            // NetworkManager.main.Spawn(instance);

            BubbleShieldProjectile projectile = instance.GetComponent<BubbleShieldProjectile>();
            if (projectile == null)
            {
                Debug.LogError("[AbilityBubbleShield] bubbleShieldPrefab is missing BubbleShieldProjectile component.");
                return;
            }

            projectile.Launch(lobDirection * config.lobForce);
        }
    }

    public struct AbilityBubbleShieldState : IPredictedData<AbilityBubbleShieldState>
    {
        public float Cooldown;
        public Vector3 LobDirection;
        public Vector3 SpawnPosition;

        public void Dispose()
        {
        }
    }

    public struct AbilityBubbleShieldInput : IPredictedData
    {
        public bool ActivatePressed;
        public Vector3 SpawnPosition;
        public Vector3 LobDirection;

        public void Dispose()
        {
        }
    }
}