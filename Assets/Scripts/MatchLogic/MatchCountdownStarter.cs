using PurrNet;
using Resonance.Assemblies.LobbySystem;
using Resonance.GameBootstrap;
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

        private NetworkedMatchDataHolder _matchDataHolder;
        private bool countdownQueued = false;

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            _matchDataHolder = FindFirstObjectByType<NetworkedMatchDataHolder>();
            // this is an optional dependency

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

        [ServerOnly]
        private void OnPlayerLoadedScene(PlayerID player, SceneID scene, bool asServer)
        {
            if (_matchDataHolder != null)
            {
                StartMatchCountdownIfAllPlayersLoadedScene();
            } else if (!countdownQueued)
            {
                QueueMatchCountdown();
            }
        }

        [ServerOnly]
        private void StartMatchCountdownIfAllPlayersLoadedScene()
        {
            if (_matchDataHolder == null) return;
            var targetScene = SceneManager.GetActiveScene();

            if (networkManager.sceneModule.TryGetSceneID(targetScene, out var sceneId))
            {
                if (networkManager.scenePlayersModule.TryGetPlayersInScene(sceneId, out var players))
                {
                    if (players.Count == _matchDataHolder.GetMemberCount())
                    {
                        Debug.Log($"[MatchStarter] All players loaded into scene {targetScene} (sceneId={sceneId}), queuing match countdown of {autoStartDelaySeconds}s");
                        QueueMatchCountdown();
                    }
                }
            }
        }

        private void QueueMatchCountdown()
        {
            countdownQueued = true;
            Invoke(nameof(StartMatchCountdown), autoStartDelaySeconds);
        }

        private void StartMatchCountdown()
        {
            var activeRoundManager = MatchLogicNetworkAdapter.Instance?.GetTemporaryActiveRoundManagerReference();
            if (activeRoundManager != null)
            {
                activeRoundManager.StartMatchCountdown();
                Debug.Log("[MatchStarter] Match countdown started.");
            }
            else
            {
                Debug.LogError("[MatchStarter] Active round manager is null! Make sure MatchLogicNetworkAdapter is in the scene.");
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
