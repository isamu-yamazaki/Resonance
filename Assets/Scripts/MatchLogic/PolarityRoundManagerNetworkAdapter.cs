using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.Polarity;
using Resonance.Assemblies.SharedGameLogic;
using UnityEngine;

namespace Resonance.Match
{
    [Serializable]
    public class PolarityRoundManagerNetworkAdapter : BaseRoundManagerNetworkAdapter
    {
        private PolarityRoundManager.PolarityRoundManagerConfig config;
        private PolarityRoundManager polarityRoundManager;

        #region Cached Client-Side State
        private int cachedTeamEliminationsToWin;
        private double cachedSecondsUntilRoleSwitch;

        public int TeamEliminationsToWin => cachedTeamEliminationsToWin;
        public double SecondsRemainingUntilRoleSwitch => cachedSecondsUntilRoleSwitch;
        #endregion

        #region Events
        public event Action<TeamId> OnMatchEnd;
        public event Action OnRoleSwitch;
        public event Action<double> OnRoleSwitchTimerElapsed;
        #endregion

        #region Constructor
        public PolarityRoundManagerNetworkAdapter(
            MatchStatNetworkAdapter adapter,
            PolarityRoundManager.PolarityRoundManagerConfig config)
            : base(adapter)
        {
            this.config = config;
        }

        public PolarityRoundManagerNetworkAdapter(MatchStatNetworkAdapter adapter)
            : this(adapter, PolarityRoundManager.PolarityRoundManagerConfig.Default)
        {
        }
        #endregion

        #region Initialization
        protected override bool HasRoundManager() => polarityRoundManager != null;

        protected override void CreateRoundManager(MatchStatTracker tracker)
        {
            Debug.Log("[PolarityRoundManagerNetworkAdapter] MatchStatTracker instance received, creating PolarityRoundManager and attaching subscribers");
            polarityRoundManager = new PolarityRoundManager(tracker, config);

            polarityRoundManager.OnMatchStart += OnPolarityMatchStart;
            polarityRoundManager.OnMatchEnd += OnPolarityMatchEnd;
            polarityRoundManager.OnMatchCountdownStart += HandleMatchCountdownStart;
            polarityRoundManager.OnMatchStateChange += HandleMatchStateChange;
            polarityRoundManager.OnRoleSwitch += OnPolarityRoleSwitch;
            polarityRoundManager.OnRoleSwitchTimerElapsed += OnPolarityRoleSwitchTimerElapsed;
        }

        protected override void DestroyRoundManager()
        {
            if (polarityRoundManager != null)
            {
                polarityRoundManager.OnMatchStart -= OnPolarityMatchStart;
                polarityRoundManager.OnMatchEnd -= OnPolarityMatchEnd;
                polarityRoundManager.OnMatchCountdownStart -= HandleMatchCountdownStart;
                polarityRoundManager.OnMatchStateChange -= HandleMatchStateChange;
                polarityRoundManager.OnRoleSwitch -= OnPolarityRoleSwitch;
                polarityRoundManager.OnRoleSwitchTimerElapsed -= OnPolarityRoleSwitchTimerElapsed;
                polarityRoundManager = null;
            }
        }
        #endregion

        #region Base Class Abstract Implementations
        protected override void CacheMatchStartParam(int param) => cachedTeamEliminationsToWin = param;
        protected override void CallStartMatchCountdown() => polarityRoundManager?.StartMatchCountdown();
        protected override bool GetRoundManagerIsMatchActive() => polarityRoundManager?.IsMatchActive ?? false;
        protected override bool GetRoundManagerIsMatchEnded() => polarityRoundManager?.IsMatchEnded ?? false;
        #endregion

        #region Server Event Handlers
        private void OnPolarityMatchStart()
        {
            FireMatchStartObservers(polarityRoundManager.TeamEliminationsToWin);
        }

        private void OnPolarityMatchEnd(TeamId winningTeamId)
        {
            FireMatchEndObservers((int)winningTeamId);
        }

        private void OnPolarityRoleSwitch()
        {
            FireRoleSwitchObservers();
        }

        private void OnPolarityRoleSwitchTimerElapsed(double secondsRemaining)
        {
            FireRoleSwitchTimerElapsedObservers(secondsRemaining);
        }
        #endregion

        #region Server to Client RPCs
        [ObserversRpc]
        private void FireMatchEndObservers(int winningTeamId)
        {
            Debug.Log($"[PolarityRoundManagerNetworkAdapter] Match ended, winning team: {winningTeamId}");
            OnMatchEnd?.Invoke((TeamId)winningTeamId);
        }

        [ObserversRpc]
        private void FireRoleSwitchObservers()
        {
            Debug.Log("[PolarityRoundManagerNetworkAdapter] Roles switched");
            OnRoleSwitch?.Invoke();
        }

        [ObserversRpc]
        private void FireRoleSwitchTimerElapsedObservers(double secondsRemaining)
        {
            cachedSecondsUntilRoleSwitch = secondsRemaining;
            OnRoleSwitchTimerElapsed?.Invoke(secondsRemaining);
        }
        #endregion

        #region Client to Server Actions (Polarity-Specific Public API)
        public void RegisterPlayersForTeamA(List<PlayerID> players)
        {
            RegisterPlayersForTeamA_Server(OwnerIDExtractor.PlayerIdListToUlongList(players));
        }

        [ServerRpc]
        private void RegisterPlayersForTeamA_Server(List<ulong> players)
        {
            polarityRoundManager?.RegisterPlayersForTeamA(new HashSet<ulong>(players));
        }

        public void RegisterPlayersForTeamB(List<PlayerID> players)
        {
            RegisterPlayersForTeamB_Server(OwnerIDExtractor.PlayerIdListToUlongList(players));
        }

        [ServerRpc]
        private void RegisterPlayersForTeamB_Server(List<ulong> players)
        {
            polarityRoundManager?.RegisterPlayersForTeamB(new HashSet<ulong>(players));
        }

        public void EndMatch(TeamId winningTeamId)
        {
            Debug.Log($"[PolarityRoundManagerNetworkAdapter] EndMatch requested, winning team: {winningTeamId}");
            EndMatch_Server((int)winningTeamId);
        }

        [ServerRpc]
        private void EndMatch_Server(int winningTeamId)
        {
            _ = polarityRoundManager?.EndMatch((TeamId)winningTeamId);
        }
        #endregion

        #region Getters (Client Callable)
        [ServerRpc]
        public async Task<Dictionary<int, List<PlayerRanking>>> GetLeaderboard_Server()
        {
            var leaderboard = polarityRoundManager?.GetLeaderboard() ?? new Dictionary<TeamId, List<PlayerRanking>>();
            var result = new Dictionary<int, List<PlayerRanking>>();
            foreach (var kvp in leaderboard)
            {
                result[(int)kvp.Key] = kvp.Value;
            }
            return result;
        }

        public async Task<Dictionary<TeamId, List<PlayerRanking>>> GetLeaderboard()
        {
            var raw = await GetLeaderboard_Server();
            var result = new Dictionary<TeamId, List<PlayerRanking>>();
            foreach (var kvp in raw)
            {
                result[(TeamId)kvp.Key] = kvp.Value;
            }
            return result;
        }

        public async Task<PolarityRoundManager.Team> GetTeam(TeamId teamId)
        {
            return await GetTeam_Server((int)teamId);
        }

        [ServerRpc]
        private async Task<PolarityRoundManager.Team> GetTeam_Server(int teamId)
        {
            return polarityRoundManager != null
                ? polarityRoundManager.GetTeam((TeamId)teamId)
                : default;
        }

        public async Task<List<PlayerID>> GetPlayersForTeam(TeamId teamId)
        {
            var ulongs = await GetPlayersForTeam_Server((int)teamId);
            var result = new List<PlayerID>();
            foreach (var id in ulongs)
            {
                result.Add(OwnerIDExtractor.UlongToPlayerId(id));
            }
            return result;
        }

        [ServerRpc]
        private async Task<List<ulong>> GetPlayersForTeam_Server(int teamId)
        {
            var players = polarityRoundManager?.GetPlayersForTeam((TeamId)teamId) ?? new HashSet<ulong>();
            return new List<ulong>(players);
        }

        [ServerRpc]
        public async Task<double> GetSecondsUntilNextRoleSwitch()
        {
            return polarityRoundManager?.SecondsUntilNextRoleSwitch ?? 0;
        }
        #endregion
    }
}
