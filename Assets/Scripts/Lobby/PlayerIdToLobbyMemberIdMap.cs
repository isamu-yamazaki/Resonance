using PurrNet;
using UnityEngine;

namespace Resonance.LobbySystem
{
    public class PlayerIdToLobbyMemberIdMap : NetworkBehaviour
    {
        [SerializeField] private SyncDictionary<PlayerID, string> lobbyMemberIdsByPlayerId = new();

        private LobbyDataHolder lobbyDataHolder;

        public void RegisterLobbyMemberIdWithPlayerId(PlayerID playerId)
        {
            lobbyMemberIdsByPlayerId.Add(playerId, lobbyDataHolder.LocalUserId);
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
            lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (lobbyDataHolder == null)
            {
                Debug.LogError($"[{GetType()}] Unable to find {nameof(LobbyDataHolder)} component");
                Destroy(this);
            }

            if (InstanceHandler.TryGetInstance<PlayerIdToLobbyMemberIdMap>(out var _))
            {
                Destroy(this);
            }
            InstanceHandler.RegisterInstance(this);
            DontDestroyOnLoad(this);
        }

        protected override void OnDestroy()
        {
            if (InstanceHandler.TryGetInstance<PlayerIdToLobbyMemberIdMap>(out var _))
            {
                InstanceHandler.UnregisterInstance<PlayerIdToLobbyMemberIdMap>();
            }
        }
    }
}
