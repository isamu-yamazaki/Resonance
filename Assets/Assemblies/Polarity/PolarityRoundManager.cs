using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using PurrNet.Packing;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.SharedGameLogic;

namespace Resonance.Assemblies.Polarity
{
    public enum TeamId { TeamA, TeamB }

    public class PolarityRoundManager : BaseRoundManager
    {
        #region Custom data types
        [System.Serializable]
        public struct Team : IPackedAuto
        {
            public PolarityTeamRole currentRole;
            public int eliminations;
        }

        public enum PolarityTeamRole
        {
            Taggers,
            Runners,
        }
        #endregion

        #region Configuration
        [System.Serializable]
        public struct PolarityRoundManagerConfig : IPackedAuto
        {
            public int timeBetweenRoleSwitchSeconds;
            public int teamEliminationsToWin;
            public float matchStartCountdownSeconds;

            public static PolarityRoundManagerConfig Default => new()
            {
                timeBetweenRoleSwitchSeconds = 90,
                teamEliminationsToWin = 10,
                matchStartCountdownSeconds = 5f,
            };
        }
        private int timeBetweenRoleSwitchSeconds = 90;
        private int teamEliminationsToWin = 10;
        #endregion

        #region State
        private DateTime? timeOfLastRoleSwitch = null;
        private Timer roleSwitchCheckTimer = null;
        private Dictionary<TeamId, Team> teams;
        private Dictionary<TeamId, HashSet<ulong>> teamPlayers;
        #endregion

        #region Properties
        public int TeamEliminationsToWin => teamEliminationsToWin;
        public IReadOnlyDictionary<TeamId, Team> Teams => teams;
        public Team GetTeam(TeamId teamId) => teams[teamId];
        public HashSet<ulong> GetPlayersForTeam(TeamId teamId) => teamPlayers[teamId];
        public DateTime? TimeOfLastRoleSwitch => timeOfLastRoleSwitch;
        public double SecondsUntilNextRoleSwitch
        {
            get
            {
                if (!IsMatchActive || timeOfLastRoleSwitch == null) { return 0; }
                var elapsed = (DateTime.Now - timeOfLastRoleSwitch.Value).TotalSeconds;
                var remaining = timeBetweenRoleSwitchSeconds - elapsed;
                return remaining > 0 ? remaining : 0;
            }
        }
        #endregion

        #region Events
        public event Action<TeamId> OnMatchEnd;
        public event Action OnRoleSwitch;
        public event Action<double> OnRoleSwitchTimerElapsed;
        #endregion

        #region Startup
        public PolarityRoundManager(MatchStatTracker tracker)
            : this(tracker, PolarityRoundManagerConfig.Default)
        {
        }

        public PolarityRoundManager(MatchStatTracker tracker, PolarityRoundManagerConfig config)
            : base(tracker, config.matchStartCountdownSeconds)
        {
            timeBetweenRoleSwitchSeconds = config.timeBetweenRoleSwitchSeconds;
            teamEliminationsToWin = config.teamEliminationsToWin;

            teams = new()
            {
                [TeamId.TeamA] = new Team { currentRole = PolarityTeamRole.Taggers, eliminations = 0 },
                [TeamId.TeamB] = new Team { currentRole = PolarityTeamRole.Runners, eliminations = 0 },
            };
            teamPlayers = new()
            {
                [TeamId.TeamA] = new HashSet<ulong>(),
                [TeamId.TeamB] = new HashSet<ulong>(),
            };

            SubscribeToEvents();
        }
        #endregion

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            if (matchStatTracker != null)
            {
                matchStatTracker.OnPlayerKill += OnPlayerKilled;
            }
        }
        #endregion

        #region Player Registration
        public void RegisterPlayersForTeamA(HashSet<ulong> players) => teamPlayers[TeamId.TeamA] = players;
        public void RegisterPlayersForTeamB(HashSet<ulong> players) => teamPlayers[TeamId.TeamB] = players;
        #endregion

        #region Match Control
        public override void StartMatchWithoutCountdown()
        {
            if (IsMatchActive) { return; }

            var oldMatchState = matchState;
            matchState = BaseMatchState.MatchActive;

            teams[TeamId.TeamA] = new Team { currentRole = PolarityTeamRole.Taggers, eliminations = 0 };
            teams[TeamId.TeamB] = new Team { currentRole = PolarityTeamRole.Runners, eliminations = 0 };
            timeOfLastRoleSwitch = DateTime.Now;

            matchStatTracker?.ResetAllStats();

            RaiseMatchStart();
            RaiseMatchStateChange(oldMatchState, matchState);

            SetRoleSwitchCheckTimer();
        }

        private void SetRoleSwitchCheckTimer()
        {
            roleSwitchCheckTimer?.Stop();
            roleSwitchCheckTimer?.Dispose();

            roleSwitchCheckTimer = new Timer(1000);
            roleSwitchCheckTimer.Elapsed += (_, _) =>
            {
                OnRoleSwitchTimerElapsed?.Invoke(SecondsUntilNextRoleSwitch);
                CheckForRoleSwitch();
            };
            roleSwitchCheckTimer.AutoReset = true;
            roleSwitchCheckTimer.Enabled = true;
        }

        public void SwitchRoles()
        {
            if (!IsMatchActive) { return; }

            var roleA = teams[TeamId.TeamA].currentRole;
            var teamAData = teams[TeamId.TeamA];
            teamAData.currentRole = teams[TeamId.TeamB].currentRole;
            teams[TeamId.TeamA] = teamAData;

            var teamBData = teams[TeamId.TeamB];
            teamBData.currentRole = roleA;
            teams[TeamId.TeamB] = teamBData;

            timeOfLastRoleSwitch = DateTime.Now;

            OnRoleSwitch?.Invoke();
        }

        public async Task EndMatch(TeamId winningTeamId)
        {
            if (matchState != BaseMatchState.MatchActive) { return; }

            matchState = BaseMatchState.MatchEnded;
            RaiseMatchStateChange(BaseMatchState.MatchActive, matchState);
            OnMatchEnd?.Invoke(winningTeamId);

            roleSwitchCheckTimer?.Stop();
            roleSwitchCheckTimer?.Dispose();
        }

        private void CheckForRoleSwitch()
        {
            if (!IsMatchActive || timeOfLastRoleSwitch == null) { return; }

            var elapsed = (DateTime.Now - timeOfLastRoleSwitch.Value).TotalSeconds;
            if (elapsed >= timeBetweenRoleSwitchSeconds)
            {
                SwitchRoles();
            }
        }
        #endregion

        #region Kill Event Handling
        private void IncrementEliminationsAndCheckWin(TeamId teamId)
        {
            var team = teams[teamId];
            team.eliminations++;
            teams[teamId] = team;
            if (team.eliminations >= teamEliminationsToWin)
            {
                _ = EndMatch(teamId);
            }
        }

        private void OnPlayerKilled(ulong attacker, ulong victim)
        {
            if (!IsMatchActive || IsMatchEnded) { return; }

            foreach (var kvp in teamPlayers)
            {
                if (kvp.Value.Contains(attacker))
                {
                    IncrementEliminationsAndCheckWin(kvp.Key);
                    return;
                }
            }
        }
        #endregion

        #region GetLeaderboard
        public Dictionary<TeamId, List<PlayerRanking>> GetLeaderboard()
        {
            var result = new Dictionary<TeamId, List<PlayerRanking>>();
            foreach (var teamId in teamPlayers.Keys)
            {
                result[teamId] = BuildRankingsForTeam(teamId);
            }
            return result;
        }

        private List<PlayerRanking> BuildRankingsForTeam(TeamId teamId)
        {
            var rankings = new List<PlayerRanking>();
            foreach (var playerId in teamPlayers[teamId])
            {
                if (matchStatTracker?.GetStats(playerId) is PlayerMatchStats stats)
                {
                    rankings.Add(new PlayerRanking { player = playerId, stats = stats });
                }
            }
            return rankings
                .OrderByDescending(r => r.stats.kills)
                .ThenByDescending(r => r.stats.KDA)
                .ThenBy(r => r.stats.deaths)
                .ToList();
        }
        #endregion
    }
}
