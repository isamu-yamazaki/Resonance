using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PurrNet;
using Resonance.Assemblies.Arena;
using Resonance.Assemblies.MatchStat;
using UnityEngine;

namespace Resonance.Match
{
    /// <summary>
    /// NetworkModule adapter which bridges ArenaRoundManager with PurrNet, handling
    /// RPC calls appropriately. On receiving MatchStatNetworkAdapter, subscribes
    /// to the creation of MatchStatTracker to receive match stat events.
    /// </summary>
    [Serializable]
    public class ArenaRoundManagerNetworkAdapter : NetworkModule
    {
        private MatchStatNetworkAdapter matchStatNetworkAdapter;
        private ArenaRoundManager.ArenaRoundManagerConfig _config;
        private ArenaRoundManager arenaRoundManager;

        #region Cached Client-Side State
        private int cachedEliminationsToWin;
        private ArenaMatchState cachedMatchState;
        private double cachedSecondsRemainingForMatch;

        public int EliminationsToWin => cachedEliminationsToWin;
        public bool IsMatchActive => cachedMatchState == ArenaMatchState.MatchActive;
        public bool IsMatchEnded => cachedMatchState == ArenaMatchState.MatchEnded;
        public double SecondsRemainingForMatch => cachedSecondsRemainingForMatch;
        #endregion

        #region Events
        public event Action<ArenaMatchState, ArenaMatchState> OnMatchStateChange;  // Old state, new state
        public event Action<float> OnMatchCountdownStart;
        public event Action OnMatchStart;
        public event Action<PlayerID?> OnMatchEnd;
        public event Action<PlayerID, int> OnLeaderChanged;
        public event Action<double> OnMatchTimerElapsed;  // Total seconds remaining
        #endregion

        #region Constructor
        public ArenaRoundManagerNetworkAdapter(
            MatchStatNetworkAdapter adapter,
            ArenaRoundManager.ArenaRoundManagerConfig config)
        {
            matchStatNetworkAdapter = adapter;
            _config = config;

            // because ArenaRoundManager depends on MatchStatTracker, we must wait for
            // matchStatNetworkAdapter to create the relevant instance
            matchStatNetworkAdapter.OnMatchStatTrackerCreated.AddListener(OnMatchStatTrackerCreated);
        }

        public ArenaRoundManagerNetworkAdapter(MatchStatNetworkAdapter adapter)
            : this(adapter, ArenaRoundManager.ArenaRoundManagerConfig.Default)
        {
        }
        #endregion

        #region Initialization 
        public override void OnSpawn(bool asServer)
        {
            base.OnSpawn(asServer);
        }

        public override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);

            matchStatNetworkAdapter?.OnMatchStatTrackerCreated.RemoveListener(OnMatchStatTrackerCreated);

            if (asServer && arenaRoundManager != null)
            {
                DestroyArenaRoundManager();
            }
        }


        private void OnMatchStatTrackerCreated(MatchStatTracker tracker)
        {
            if (arenaRoundManager == null)
            {
                CreateArenaRoundManagerWithMatchStatTracker(tracker);
            }
            else
            {
                Debug.LogWarning("[ArenaRoundManagerNetworkAdapter] MatchStatTracker instance received but ArenaRoundManager instance already exists; re-creating with new match stat tracker");
                DestroyArenaRoundManager();
                CreateArenaRoundManagerWithMatchStatTracker(tracker);
            }
        }

        private void DestroyArenaRoundManager()
        {
            if (arenaRoundManager != null)
            {
                arenaRoundManager.OnMatchStart -= OnArenaMatchStart;
                arenaRoundManager.OnMatchEnd -= OnArenaMatchEnd;
                arenaRoundManager.OnLeaderChanged -= OnArenaLeaderChanged;
                arenaRoundManager.OnMatchCountdownStart -= OnArenaMatchCountdownStart;
                arenaRoundManager.OnMatchStateChange -= OnArenaMatchStateChange;
                arenaRoundManager.OnMatchTimerElapsed -= OnArenaMatchTimerElapsed;
                arenaRoundManager = null;
            }
        }

        private void CreateArenaRoundManagerWithMatchStatTracker(MatchStatTracker tracker)
        {
            Debug.Log("[ArenaRoundManagerNetworkAdapter] MatchStatTracker instance received, creating ArenaRoundManager and attaching subscribers");
            arenaRoundManager = new ArenaRoundManager(tracker, _config);

            arenaRoundManager.OnMatchStart += OnArenaMatchStart;
            arenaRoundManager.OnMatchEnd += OnArenaMatchEnd;
            arenaRoundManager.OnLeaderChanged += OnArenaLeaderChanged;
            arenaRoundManager.OnMatchCountdownStart += OnArenaMatchCountdownStart;
            arenaRoundManager.OnMatchStateChange += OnArenaMatchStateChange;
            arenaRoundManager.OnMatchTimerElapsed += OnArenaMatchTimerElapsed;
        }

        #endregion

        #region Server Event Handlers
        private void OnArenaMatchCountdownStart(float countdownSeconds)
        {
            FireMatchCountdownStartObservers(countdownSeconds);
        }

        private void OnArenaMatchStart()
        {
            FireMatchStartObservers(arenaRoundManager.EliminationsToWin);
        }

        private void OnArenaMatchEnd(ulong? winner)
        {
            FireMatchEndObservers(winner);
        }

        private void OnArenaLeaderChanged(ulong newLeader, int eliminations)
        {
            FireLeaderChangedObservers(newLeader, eliminations);
        }

        private void OnArenaMatchStateChange(ArenaMatchState oldState, ArenaMatchState newState)
        {
            FireMatchStateChangeObservers((int)oldState, (int)newState);
        }

        private void OnArenaMatchTimerElapsed(double secondsRemaining)
        {
            FireMatchTimerElapsedObservers(secondsRemaining);
        }


        #endregion

        #region Server to Client RPCs
        [ObserversRpc]
        private void FireMatchCountdownStartObservers(float countdownSeconds)
        {
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] Match countdown of {countdownSeconds}s started");
            OnMatchCountdownStart?.Invoke(countdownSeconds);
        }

        [ObserversRpc]
        private void FireMatchStartObservers(int eliminationsToWin)
        {
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] Match started, eliminationsToWin: {eliminationsToWin}");
            cachedEliminationsToWin = eliminationsToWin;
            OnMatchStart?.Invoke();
        }

        [ObserversRpc]
        private void FireMatchEndObservers(ulong? winner)
        {
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] Match ended, winner: {winner}");
            PlayerID? winnerPlayerId = OwnerIDExtractor.UlongNullableToPlayerIdNullable(winner);
            OnMatchEnd?.Invoke(winnerPlayerId);
        }

        [ObserversRpc]
        private void FireLeaderChangedObservers(ulong newLeader, int eliminations)
        {
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] Leader changed: {newLeader} with {eliminations} eliminations");
            OnLeaderChanged?.Invoke(
                OwnerIDExtractor.UlongToPlayerId(newLeader),
                eliminations
            );
        }

        [ObserversRpc]
        private void FireMatchStateChangeObservers(int oldState, int newState)
        {
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] Match state changed from {oldState} to {newState}");
            cachedMatchState = (ArenaMatchState)newState;
            OnMatchStateChange?.Invoke((ArenaMatchState)oldState, (ArenaMatchState)newState);
        }

        [ObserversRpc]
        private void FireMatchTimerElapsedObservers(double secondsRemaining)
        {
            cachedSecondsRemainingForMatch = secondsRemaining;
            OnMatchTimerElapsed?.Invoke(secondsRemaining);
        }

        #endregion

        #region Client to Server Actions (Public API)
        public void StartMatchCountdown()
        {
            Debug.Log("[ArenaRoundManagerNetworkAdapter] StartMatchCountdown requested");
            StartMatchCountdown_Server();
        }

        [ServerRpc]
        private void StartMatchCountdown_Server()
        {
            arenaRoundManager?.StartMatchCountdown();
        }

        /// <summary>
        /// Ends the match with a winner. Note that in most scenarios, this won't
        /// need to be called directly.
        /// </summary>
        /// <param name="winner"></param>
        public void EndMatch(PlayerID? winner)
        {
            ulong? winnerUlong = winner?.id.value;
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] EndMatch requested, winner: {winnerUlong}");
            EndMatch_Server(winnerUlong);
        }

        [ServerRpc]
        private void EndMatch_Server(ulong? winner)
        {
            arenaRoundManager?.EndMatch(winner);
        }
        #endregion

        #region Getters (Client Callable)
        [ServerRpc]
        public async Task<bool> GetIsMatchActive()
        {
            return arenaRoundManager?.IsMatchActive ?? false;
        }

        [ServerRpc]
        public async Task<bool> GetIsMatchEnded()
        {
            return arenaRoundManager?.IsMatchEnded ?? false;
        }

        [ServerRpc]
        public async Task<int> GetEliminationsToWin()
        {
            return arenaRoundManager?.EliminationsToWin ?? 0;
        }

        [ServerRpc]
        public async Task<PlayerID?> GetCurrentLeader()
        {
            return OwnerIDExtractor.UlongNullableToPlayerIdNullable(arenaRoundManager?.CurrentLeader);
        }

        [ServerRpc]
        public async Task<int> GetHighestEliminations()
        {
            return arenaRoundManager?.HighestEliminations ?? 0;
        }

        public async Task<List<PlayerRanking>> GetLeaderboard()
        {
            var leaderboardJson = await GetLeaderboard_Server();
            return JsonConvert.DeserializeObject<List<PlayerRanking>>(leaderboardJson);
        }

        [ServerRpc]
        private async Task<string> GetLeaderboard_Server()
        {
            var leaderboard = arenaRoundManager?.GetLeaderboard() ?? new List<PlayerRanking>();
            return JsonConvert.SerializeObject(leaderboard);
        }

        [ServerRpc]
        public async Task<string> GetLeaderboardString()
        {
            return arenaRoundManager?.GetLeaderboardString() ?? "";
        }

        [ServerRpc]
        public async Task<double> GetSecondsRemaining()
        {
            return arenaRoundManager?.SecondsRemainingForMatch ?? 0;
        }
        #endregion
    }
}
