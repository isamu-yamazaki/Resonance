using System;
using PurrNet;
using Resonance.LobbySystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resonance.Match
{
    /// <summary>
    /// NetworkBehaviour which starts a match countdown on the server side,
    /// once all players have joined.
    /// </summary>
    public class MatchCountdownStarter : NetworkBehaviour
    {
        [Header("Auto Start Settings")]
        [SerializeField] private bool autoStartAfterPlayersLoadedIn = true;
        [SerializeField] private float autoStartDelaySeconds = 5f; // Small delay to ensure everything is initialized

        private LobbyDataHolder lobbyDataHolder;

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (!lobbyDataHolder)
            {
                Debug.LogError($"[MatchStarter] Unable to find {nameof(LobbyDataHolder)} component; will not start match.");
            }

            if (asServer && autoStartAfterPlayersLoadedIn)
            {
                networkManager.onPlayerLoadedScene += OnPlayerLoadedScene;
            }
        }

        protected override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);

            if (asServer && autoStartAfterPlayersLoadedIn)
            {
                networkManager.onPlayerLoadedScene -= OnPlayerLoadedScene;
            }
        }

        private void OnPlayerLoadedScene(PlayerID player, SceneID scene, bool asServer)
        {
            StartMatchCountdownIfAllPlayersLoadedScene();
        }

        private void StartMatchCountdownIfAllPlayersLoadedScene()
        {
            var targetSceneName = lobbyDataHolder.CurrentLobby.SceneName;
            var targetScene = SceneManager.GetSceneByName(targetSceneName);

            if (networkManager.sceneModule.TryGetSceneID(targetScene, out var sceneId))
            {
                if (networkManager.scenePlayersModule.TryGetPlayersInScene(sceneId, out var players))
                {
                    if (players.Count == lobbyDataHolder.CurrentLobby.Members.Count)
                    {
                        Debug.Log($"[MatchStarter] All players loaded into scene {targetScene} (sceneId={sceneId}), queuing match countdown of {autoStartDelaySeconds}s");
                        QueueMatchCountdown();
                    }
                }
            }
        }

        private void QueueMatchCountdown()
        {
            Invoke(nameof(StartMatchCountdown), autoStartDelaySeconds);
        }

        private void StartMatchCountdown()
        {
            var activeRoundManager = MatchLogicNetworkAdapter.Instance.ActiveRoundManager;
            if (activeRoundManager != null)
            {
                activeRoundManager.StartMatchCountdown();
                Debug.Log("[MatchStarter] Match countdown started.");
            }
            else
            {
                Debug.LogError("[MatchStarter] MatchLogicNetworkAdapter.Instance.ActiveRoundManager is null! Make sure MatchLogicNetworkAdapter is in the scene.");
            }
        }

        // Manual start method you can call from a button or inspector
        [ContextMenu("Start Match Countdown")]
        public void StartMatchCountdownManually()
        {
            StartMatchCountdown();
        }
    }
}
