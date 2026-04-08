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
        private ArenaRatingManager arenaRatingManager;

        #region Cached Client-Side State
        private float cachedRatingToWin;
        private double cachedSecondsRemainingForMatch;

        public float RatingToWin => cachedRatingToWin;
        public double SecondsRemainingForMatch => cachedSecondsRemainingForMatch;
        #endregion

        #region Events
        public event Action<PlayerID?> OnMatchEnd;
        public event Action<PlayerID, float> OnLeaderChanged;
        public event Action OnFirstKill;
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
            if (arenaRoundManager == null)
            {
                Debug.Log("[ArenaRoundManagerNetworkAdapter] MatchStatTracker instance received, creating ArenaRoundManager and attaching subscribers");
                arenaRoundManager = new ArenaRoundManager(tracker, config);

                arenaRoundManager.OnMatchStart += OnArenaMatchStart;
                arenaRoundManager.OnMatchEnd += OnArenaMatchEnd;
                arenaRoundManager.OnLeaderChanged += OnArenaLeaderChanged;
                arenaRoundManager.OnFirstKill += OnArenaFirstKill;
                arenaRoundManager.OnMatchCountdownStart += HandleMatchCountdownStart;
                arenaRoundManager.OnMatchStateChange += HandleMatchStateChange;
                arenaRoundManager.OnMatchTimerElapsed += OnArenaMatchTimerElapsed;

                arenaRatingManager = new ArenaRatingManager(tracker, arenaRoundManager);
            }
            else
            {
                Debug.Log("[ArenaRoundManagerNetworkAdapter] MatchStatTracker instance received but ArenaRoundManager is not null");
            }
        }

        protected override void DestroyRoundManager()
        {
            if (arenaRoundManager != null)
            {
                arenaRoundManager.OnMatchStart -= OnArenaMatchStart;
                arenaRoundManager.OnMatchEnd -= OnArenaMatchEnd;
                arenaRoundManager.OnLeaderChanged -= OnArenaLeaderChanged;
                arenaRoundManager.OnFirstKill -= OnArenaFirstKill;
                arenaRoundManager.OnMatchCountdownStart -= HandleMatchCountdownStart;
                arenaRoundManager.OnMatchStateChange -= HandleMatchStateChange;
                arenaRoundManager.OnMatchTimerElapsed -= OnArenaMatchTimerElapsed;

                arenaRatingManager?.Unsubscribe();
                arenaRatingManager = null;
                arenaRoundManager = null;
            }
        }
        #endregion

        #region Base Class Abstract Implementations
        protected override void CacheMatchStartParam(int param) => cachedRatingToWin = param;
        protected override void CallStartMatchCountdown() => arenaRoundManager?.StartMatchCountdown();
        protected override bool GetRoundManagerIsMatchActive() => arenaRoundManager?.IsMatchActive ?? false;
        protected override bool GetRoundManagerIsMatchEnded() => arenaRoundManager?.IsMatchEnded ?? false;
        #endregion

        #region Server Event Handlers
        private void OnArenaMatchStart()
        {
            FireMatchStartObservers((int)arenaRoundManager.RatingToWin);
        }

        private void OnArenaMatchEnd(ulong? winner)
        {
            FireMatchEndObservers(winner);
        }

        private void OnArenaLeaderChanged(ulong newLeader, float rating)
        {
            FireLeaderChangedObservers(newLeader, rating);
        }

        private void OnArenaFirstKill()
        {
            FireFirstKillObservers();
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
        private void FireLeaderChangedObservers(ulong newLeader, float rating)
        {
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] Leader changed: {newLeader} with {rating} rating");
            OnLeaderChanged?.Invoke(
                OwnerIDExtractor.UlongToPlayerId(newLeader),
                rating
            );
        }

        [ObserversRpc]
        private void FireFirstKillObservers()
        {
            Debug.Log("[ArenaRoundManagerNetworkAdapter] First kill happened");
            OnFirstKill?.Invoke();
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
        public override async Task<int> GetMatchState()
        {
            return (int)(arenaRoundManager?.MatchState ?? BaseMatchState.Waiting);
        }

        [ServerRpc]
        public async Task<float> GetRatingToWin()
        {
            return arenaRoundManager?.RatingToWin ?? 0f;
        }

        [ServerRpc]
        public async Task<PlayerID?> GetCurrentLeader()
        {
            return OwnerIDExtractor.UlongNullableToPlayerIdNullable(arenaRoundManager?.CurrentLeader);
        }

        [ServerRpc]
        public async Task<float> GetHighestRating()
        {
            return arenaRoundManager?.HighestRating ?? 0f;
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
