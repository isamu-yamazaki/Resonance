using PurrNet;
using Resonance.LobbySystem;
using UnityEngine;
using UnityEngine.Events;

namespace Resonance.GameBootstrap
{
    public class NetworkPlayerCounter : NetworkBehaviour
    {
        public UnityEvent OnAllPlayersJoined = new();
        private LobbyDataHolder lobbyDataHolder;
        private int MemberCount => lobbyDataHolder.CurrentLobby.Members.Count;

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (!lobbyDataHolder)
            {
                Debug.LogError($"[{GetType()}] Unable to find {nameof(LobbyDataHolder)} component; scene switching will not work.");
            }

            if (asServer)
            {
                networkManager.onPlayerJoined += OnPlayerJoined;
            }
        }


        protected override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);
            if (asServer)
            {
                networkManager.onPlayerJoined -= OnPlayerJoined;
            }
        }


        private void OnPlayerJoined(PlayerID player, bool isReconnect, bool asServer)
        {
            ConditionallyFireAllPlayersEvent();
        }

        private void ConditionallyFireAllPlayersEvent()
        {
            var playerJoinedCount = networkManager.playerCount;
            if (playerJoinedCount == MemberCount)
            {
                Debug.Log("[NetworkPlayerCounter] All players joined");
                OnAllPlayersJoined.Invoke();
            }
        }
    }
}
