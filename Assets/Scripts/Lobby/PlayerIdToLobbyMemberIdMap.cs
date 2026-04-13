using PurrNet;
using Resonance.Assemblies.LobbySystem;
using UnityEngine;

namespace Resonance.LobbySystem
{
    public class PlayerIdToLobbyMemberIdMap : NetworkBehaviour
    {
        // allow easy access for consumers
        public static PlayerIdToLobbyMemberIdMap Instance
        {
            get
            {
                if (InstanceHandler.TryGetInstance<PlayerIdToLobbyMemberIdMap>(out var instance))
                {
                    return instance;
                }
                return null;
            }
        }

        [SerializeField] private SyncDictionary<PlayerID, string> lobbyMemberIdsByPlayerId = new();

        private LobbyDataHolder lobbyDataHolder;

        [ServerRpc(requireOwnership: false)]
        private void RegisterLobbyMemberIdWithPlayerId(PlayerID playerId, string lobbyMemberId)
        {
            lobbyMemberIdsByPlayerId.Add(playerId, lobbyMemberId);
        }

        private void UnregisterPlayerId(PlayerID playerId)
        {
            lobbyMemberIdsByPlayerId.Remove(playerId);
        }

        public string GetLobbyMemberId(PlayerID playerId)
        {
            if (lobbyMemberIdsByPlayerId.ContainsKey(playerId))
            {
                return lobbyMemberIdsByPlayerId[playerId];
            }
            return null;
        }

        private void Awake()
        {
            if (InstanceHandler.TryGetInstance<PlayerIdToLobbyMemberIdMap>(out var _))
            {
                Destroy(this);
                return;
            }

            lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (lobbyDataHolder == null)
            {
                Debug.LogWarning($"[{GetType()}] Unable to find {nameof(LobbyDataHolder)} component, local player will not be mapped to lobby member ID");
                return;
            }

            InstanceHandler.RegisterInstance(this);
        }

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            if (!asServer && lobbyDataHolder != null)
            {
                RegisterLobbyMemberIdWithPlayerId(networkManager.localPlayer, lobbyDataHolder.LocalUserId);
            }

            if (asServer)
            {
                networkManager.onPlayerLeft += HandlePlayerLeft;
            }
        }

        protected override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);

            if (asServer)
            {
                networkManager.onPlayerLeft -= HandlePlayerLeft;
            }
        }

        private void HandlePlayerLeft(PlayerID player, bool asServer)
        {
            if (isServer)
            {
                UnregisterPlayerId(player);
            }
        }

        protected override void OnDestroy()
        {
            if (InstanceHandler.TryGetInstance<PlayerIdToLobbyMemberIdMap>(out var instance) && instance == this)
            {
                InstanceHandler.UnregisterInstance<PlayerIdToLobbyMemberIdMap>();
            }
        }

        [ContextMenu("Get lobby member ID of local player ID")]
        private void TestGetLobbyMemberIdOfLocalPlayerId()
        {
            var lobbyMemberId = GetLobbyMemberId(networkManager.localPlayer);
            Debug.Log($"[PlayerIdToLobbyMemberIdMap] Lobby member ID of local player ID: {lobbyMemberId}");
        }
    }
}
