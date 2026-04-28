using PurrNet;
using Resonance.Combat.Augments;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.BubbleShield
{
    public class AbilityBubbleShield : NetworkBehaviour, IAugmentAbility
    {
        [Header("Shield Settings")]
        [SerializeField] private float lobForce = 12f;
        [SerializeField] private float upwardLobBias = 0.4f;
        [SerializeField] private float cooldown = 20f;

        [Header("References")]
        [SerializeField] private GameObject bubbleShieldPrefab;

#if !UNITY_SERVER
        [Header("Audio")]
        [SerializeField] private AK.Wwise.Event throwSoundEvent;
#endif

        private Camera playerCamera;
        private float currentCooldown;

        public string AbilityKey => "ability_bubbleShield";
        public string Name => "Bubble Shield";
        public string Description => "Throw a shield that blocks bullets.";
        public float MaxCooldown => cooldown;
        public float CurrentCooldown
        {
            get => currentCooldown;
            set => currentCooldown = Mathf.Clamp(value, 0f, cooldown);
        }
        public bool AbilityReady => currentCooldown <= 0f;

        public void ActivateAbility()
        {
            if (!AbilityReady) return;

            currentCooldown = cooldown;

#if !UNITY_SERVER
            throwSoundEvent?.Post(gameObject);
#endif

            Vector3 spawnPosition = playerCamera.transform.position;
            Vector3 lobDirection = Vector3.Lerp(playerCamera.transform.forward, Vector3.up, upwardLobBias).normalized;

            RequestSpawnShieldServerRpc(spawnPosition, lobDirection, NetworkManager.main.localPlayer);
        }

        private void Awake()
        {
            currentCooldown = 0f;
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();

            if (isOwner)
                playerCamera = Camera.main;
        }

        private void Update()
        {
            if (!isOwner) return;

            if (currentCooldown > 0f)
                currentCooldown -= Time.deltaTime;
        }

        [ServerRpc]
        private void RequestSpawnShieldServerRpc(Vector3 spawnPosition, Vector3 lobDirection, PlayerID ownerID)
        {
            if (bubbleShieldPrefab == null)
            {
                Debug.LogWarning("[AbilityBubbleShield] bubbleShieldPrefab is not assigned.");
                return;
            }

            GameObject instance = Instantiate(bubbleShieldPrefab, spawnPosition, Quaternion.identity);
            NetworkManager.main.Spawn(instance);

            BubbleShieldProjectile projectile = instance.GetComponent<BubbleShieldProjectile>();
            if (projectile == null)
            {
                Debug.LogError("[AbilityBubbleShield] bubbleShieldPrefab is missing BubbleShieldProjectile component.");
                return;
            }

            projectile.Launch(lobDirection * lobForce);
        }
    }
}
