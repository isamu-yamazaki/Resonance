using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PurrNet;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.UISystem;
using Resonance.Match;
using Resonance.NetworkDespawner;

namespace Resonance.UI
{
    public class MatchEndOverlayView : MonoBehaviour, IOverlayView
    {
        public static string Key => nameof(MatchEndOverlayView);
        string IOverlayView.Key => Key;

        public static MatchEndOverlayView Instance { get; private set; }

        [Header("Match End UI")]
        [SerializeField] private GameObject matchEndPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI finalStatsText;
        [SerializeField] private TextMeshProUGUI waitingForHostText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button returnToLobbyButton;

        [Header("Leaderboard")]
        [SerializeField] private Transform leaderboardContentRoot;
        [SerializeField] private LeaderboardRow leaderboardRowPrefab;

        [Header("Dependencies")]
        [SerializeField] private NetworkDespawnerSceneLoader despawnerSceneLoader;

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

        public async void PresentWinner(PlayerID? winner)
        {
            if (winnerText != null)
            {
                winnerText.text = $"{winner} Wins!";
            }

            if (waitingForHostText != null)
            {
                waitingForHostText.text = "";
            }

            var matchStats = MatchStatBridge.GetTemporaryReference();
            if (winner is PlayerID id && matchStats != null)
            {
                PlayerMatchStats? stats = await matchStats.GetStats(id);
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

            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                if (i < rankings.Count)
                {
                    _spawnedRows[i].gameObject.SetActive(true);
                    _spawnedRows[i].Setup(i + 1, rankings[i]);
                }
                else
                {
                    _spawnedRows[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
