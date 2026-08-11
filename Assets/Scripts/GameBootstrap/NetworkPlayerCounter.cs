using PurrNet;
using Resonance.Assemblies.LobbySystem;
using UnityEngine;
using UnityEngine.Events;

namespace Resonance.GameBootstrap
{
    public class NetworkPlayerCounter : NetworkBehaviour
    {
        public UnityEvent OnAllPlayersJoined = new();
        private NetworkedMatchDataHolder _dataHolder;

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            _dataHolder = FindFirstObjectByType<NetworkedMatchDataHolder>();
            if (!_dataHolder)
            {
                Debug.LogError($"[{GetType()}] Unable to find {nameof(NetworkedMatchDataHolder)} component; scene switching will not work.");
            }

            if (!asServer) return;
            networkManager.onPlayerJoined += OnPlayerJoined;
            ConditionallyFireAllPlayersEvent();
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
            Debug.Log($"[{nameof(NetworkPlayerCounter)}] Player {player} joined");
            // PurrNet raises onPlayerJoined twice on a host (once per perspective)
            // only act on the server-perspective invocation so the UnityEvent fires once
            if (!asServer)
            {
                return;
            }
            ConditionallyFireAllPlayersEvent();
        }

        [ServerOnly]
        private void ConditionallyFireAllPlayersEvent()
        {
            var playerJoinedCount = networkManager.playerCount;
            var memberCount = _dataHolder.GetMemberCount();
            if (_dataHolder.Initialized && playerJoinedCount == memberCount)
            {
                Debug.Log($"[{nameof(NetworkPlayerCounter)}] All players joined");
                OnAllPlayersJoined.Invoke();
            }
        }
    }
}
