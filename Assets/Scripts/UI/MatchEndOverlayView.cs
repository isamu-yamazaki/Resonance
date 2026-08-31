using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PurrNet;
using Resonance.Assemblies.LobbySystem;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.UISystem;
using Resonance.GameBootstrap;
using Resonance.Match;
using Resonance.NetworkDespawner;

namespace Resonance.UI
{
    public class MatchEndOverlayView : MonoBehaviour, IOverlayView
    {
        public static string Key => nameof(MatchEndOverlayView);
        string IOverlayView.Key => Key;

        public static MatchEndOverlayView Instance { get; private set; }

        [Header("Match End UI")] [SerializeField]
        private GameObject matchEndPanel;

        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI finalStatsText;
        [SerializeField] private TextMeshProUGUI waitingForHostText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button returnToLobbyButton;

        [Header("Leaderboard")] [SerializeField]
        private Transform leaderboardContentRoot;

        [SerializeField] private LeaderboardRow leaderboardRowPrefab;

        [Header("Dependencies")] [SerializeField]
        private NetworkDespawnerSceneLoader despawnerSceneLoader;

        private NetworkedMatchDataHolder _matchDataHolder;

        private Action dismiss;
        private readonly List<LeaderboardRow> _spawnedRows = new();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (despawnerSceneLoader == null)
            {
                despawnerSceneLoader = FindFirstObjectByType<NetworkDespawnerSceneLoader>();
            }

            if (_matchDataHolder == null)
            {
                _matchDataHolder = FindFirstObjectByType<NetworkedMatchDataHolder>();
            }

            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(false);
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void OnShow(OverlayViewActions viewActions)
        {
            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(true);
            }

            dismiss = viewActions.Dismiss;

            RenderLeaderboard();
        }

        public void OnHide()
        {
            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(false);
            }

            dismiss = null;
        }

        public async Task PresentWinner(PlayerID? winner)
        {
            if (winnerText != null)
            {
                if (!winner.HasValue)
                {
                    winnerText.text = "No Winner.";
                }
                else
                {
                    var identity = PlayerIdToMatchMemberIdMap.Instance?.GetPlayerIdentityForPlayerID(winner.Value);
                    if (identity.HasValue && _matchDataHolder != null)
                    {
                        var displayName = await _matchDataHolder.GetDisplayName(identity.Value);
                        winnerText.text = $"{displayName ?? winner.ToString()} Wins!";
                    }
                    else
                    {
                        winnerText.text = $"{winner.ToString()} wins!";
                    }
                }
            }

            if (waitingForHostText != null)
            {
                waitingForHostText.text = "";
            }

            var matchStats = MatchStatBridge.GetTemporaryReference();
            if (winner.HasValue && matchStats != null)
            {
                PlayerMatchStats? stats = await matchStats.GetStats(winner.Value);
                if (stats != null && finalStatsText != null)
                {
                    finalStatsText.text = $"Final Score: {stats?.kills} Kills";
                }
            }
        }

        private void OnPlayAgainClicked()
        {
            Time.timeScale = 1f;

            var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
            if (arenaRoundManager != null)
            {
                arenaRoundManager.StartMatchCountdown();
            }
        }

        private void OnReturnToLobbyClicked()
        {
            if (despawnerSceneLoader != null)
            {
                despawnerSceneLoader.LoadNetworkDespawnerSceneForEveryone();
            }
        }

        private void RenderLeaderboard()
        {
            if (leaderboardContentRoot == null || leaderboardRowPrefab == null) return;

            var model = MatchStatsModel.Instance;
            if (model == null)
            {
                Debug.LogError("MatchStatsModel not found");
                return;
            }

            var rankings = model.Rankings.Value;

            while (_spawnedRows.Count < rankings.Count)
            {
                var row = Instantiate(leaderboardRowPrefab, leaderboardContentRoot);
                _spawnedRows.Add(row);
            }

            var playerIdMap = PlayerIdToLobbyMemberIdMap.Instance;

            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                if (i < rankings.Count)
                {
                    var ranking = rankings[i];
                    var playerId = OwnerIDExtractor.UlongToPlayerId(ranking.player);
                    var displayName = playerIdMap?.GetDisplayName(playerId);

                    _spawnedRows[i].gameObject.SetActive(true);
                    _spawnedRows[i].Setup(i + 1, ranking, displayName);
                }
                else
                {
                    _spawnedRows[i].gameObject.SetActive(false);
                }
            }
        }
    }
}