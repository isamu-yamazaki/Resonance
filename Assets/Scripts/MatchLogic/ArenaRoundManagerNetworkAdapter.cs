using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Assemblies.Arena;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.SharedGameLogic;
using UnityEngine;

namespace Resonance.Match
{
    /// <summary>
    /// NetworkModule adapter which bridges ArenaRoundManager with PurrNet, handling
    /// RPC calls appropriately. On receiving MatchStatNetworkAdapter, subscribes
    /// to the creation of MatchStatTracker to receive match stat events.
    /// </summary>
    [Serializable]
    public class ArenaRoundManagerNetworkAdapter : BaseRoundManagerNetworkAdapter
    {
        private ArenaRoundManager.ArenaRoundManagerConfig config;
        private ArenaRoundManager arenaRoundManager;

        #region Cached Client-Side State
        private int cachedEliminationsToWin;
        private double cachedSecondsRemainingForMatch;

        public int EliminationsToWin => cachedEliminationsToWin;
        public double SecondsRemainingForMatch => cachedSecondsRemainingForMatch;
        #endregion

        #region Events
        public event Action<PlayerID?> OnMatchEnd;
        public event Action<PlayerID, int> OnLeaderChanged;
        public event Action<double> OnMatchTimerElapsed;
        #endregion

        #region Constructor
        public ArenaRoundManagerNetworkAdapter(
            MatchStatNetworkAdapter adapter,
            ArenaRoundManager.ArenaRoundManagerConfig config)
            : base(adapter)
        {
            this.config = config;
        }

        public ArenaRoundManagerNetworkAdapter(MatchStatNetworkAdapter adapter)
            : this(adapter, ArenaRoundManager.ArenaRoundManagerConfig.Default)
        {
        }
        #endregion

        #region Initialization
        protected override bool HasRoundManager() => arenaRoundManager != null;

        protected override void CreateRoundManager(MatchStatTracker tracker)
        {
            Debug.Log("[ArenaRoundManagerNetworkAdapter] MatchStatTracker instance received, creating ArenaRoundManager and attaching subscribers");
            arenaRoundManager = new ArenaRoundManager(tracker, config);

            arenaRoundManager.OnMatchStart += OnArenaMatchStart;
            arenaRoundManager.OnMatchEnd += OnArenaMatchEnd;
            arenaRoundManager.OnLeaderChanged += OnArenaLeaderChanged;
            arenaRoundManager.OnMatchCountdownStart += HandleMatchCountdownStart;
            arenaRoundManager.OnMatchStateChange += HandleMatchStateChange;
            arenaRoundManager.OnMatchTimerElapsed += OnArenaMatchTimerElapsed;
        }

        protected override void DestroyRoundManager()
        {
            if (arenaRoundManager != null)
            {
                arenaRoundManager.OnMatchStart -= OnArenaMatchStart;
                arenaRoundManager.OnMatchEnd -= OnArenaMatchEnd;
                arenaRoundManager.OnLeaderChanged -= OnArenaLeaderChanged;
                arenaRoundManager.OnMatchCountdownStart -= HandleMatchCountdownStart;
                arenaRoundManager.OnMatchStateChange -= HandleMatchStateChange;
                arenaRoundManager.OnMatchTimerElapsed -= OnArenaMatchTimerElapsed;
                arenaRoundManager = null;
            }
        }
        #endregion

        #region Base Class Abstract Implementations
        protected override void CacheMatchStartParam(int param) => cachedEliminationsToWin = param;
        protected override void CallStartMatchCountdown() => arenaRoundManager?.StartMatchCountdown();
        protected override bool GetRoundManagerIsMatchActive() => arenaRoundManager?.IsMatchActive ?? false;
        protected override bool GetRoundManagerIsMatchEnded() => arenaRoundManager?.IsMatchEnded ?? false;
        #endregion

        #region Server Event Handlers
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

        private void OnArenaMatchTimerElapsed(double secondsRemaining)
        {
            FireMatchTimerElapsedObservers(secondsRemaining);
        }
        #endregion

        #region Server to Client RPCs
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
        private void FireMatchTimerElapsedObservers(double secondsRemaining)
        {
            cachedSecondsRemainingForMatch = secondsRemaining;
            OnMatchTimerElapsed?.Invoke(secondsRemaining);
        }
        #endregion

        #region Client to Server Actions (Arena-Specific Public API)
        /// <summary>
        /// Ends the match with a winner. Note that in most scenarios, this won't
        /// need to be called directly.
        /// </summary>
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

        [ServerRpc]
        public async Task<List<PlayerRanking>> GetLeaderboard()
        {
            return arenaRoundManager?.GetLeaderboard() ?? new List<PlayerRanking>();
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
