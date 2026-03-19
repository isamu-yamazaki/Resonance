using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.SharedGameLogic;

namespace Resonance.Assemblies.Arena
{
    public class ArenaRoundManager : BaseRoundManager
    {
        #region Configuration
        public struct ArenaRoundManagerConfig
        {
            public float ratingToWin;
            public bool autoStartNextMatch;
            public float autoStartDelaySeconds;
            public float matchStartCountdownSeconds;
            public float matchDurationSeconds;

            public static ArenaRoundManagerConfig Default => new()
            {
                ratingToWin =  2000f,
                autoStartNextMatch = false,
                autoStartDelaySeconds = 5f,
                matchStartCountdownSeconds = 5f,
                matchDurationSeconds = 300f,
            };
        }
        private float ratingToWin = 2000f;
        private float autoStartDelaySeconds = 3f;
        private bool autoStartNextMatch = false;
        private float matchDurationSeconds = 300f;
        #endregion

        #region State
        private ulong? currentLeader = null;
        private float highestRating = 0;
        private bool firstKillHappened = false;
        private DateTime timeOfLastMatchStart = default;
        private Timer matchEndCheckTimer = null;
        #endregion

        #region Events
        public event Action<ulong?> OnMatchEnd;
        public event Action<ulong, float> OnLeaderChanged;
        public event Action OnFirstKill;
        public event Action<double> OnMatchTimerElapsed;
        #endregion

        #region Properties
        public float RatingToWin => ratingToWin;
        public ulong? CurrentLeader => currentLeader;
        public float HighestRating => highestRating;
        public DateTime TimeOfMatchEnd => timeOfLastMatchStart.AddSeconds(matchDurationSeconds);
        public double SecondsRemainingForMatch
        {
            get
            {
                var calculatedTimeRemaining = (TimeOfMatchEnd - DateTime.Now).TotalSeconds;
                if (calculatedTimeRemaining > 0)
                {
                    return calculatedTimeRemaining;
                }
                return 0;
            }
        }
        #endregion

        public ArenaRoundManager(MatchStatTracker tracker)
            : this(tracker, ArenaRoundManagerConfig.Default)
        {
        }

        public ArenaRoundManager(MatchStatTracker tracker, ArenaRoundManagerConfig config)
            : base(tracker, config.matchStartCountdownSeconds)
        {
            autoStartNextMatch = config.autoStartNextMatch;
            ratingToWin = config.ratingToWin;
            autoStartDelaySeconds = config.autoStartDelaySeconds;
            matchDurationSeconds = config.matchDurationSeconds;

            SubscribeToEvents();
        }

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            if (matchStatTracker != null)
            {
                matchStatTracker.OnPlayerKill += OnPlayerKilled;
                matchStatTracker.OnAllStatsUpdated += OnAllStatsUpdated;
            }
        }
        #endregion

        #region Match Control
        public override void StartMatchWithoutCountdown()
        {
            if (IsMatchActive) { return; }

            var oldMatchState = matchState;
            matchState = BaseMatchState.MatchActive;

            currentLeader = null;
            highestRating = 0;
            firstKillHappened = false;
            timeOfLastMatchStart = DateTime.Now;

            matchStatTracker?.ResetAllStats();

            RaiseMatchStart();
            RaiseMatchStateChange(oldMatchState, matchState);

            SetCheckForMatchEndTimer();
        }

        private void SetCheckForMatchEndTimer()
        {
            matchEndCheckTimer = new Timer(1000);
            matchEndCheckTimer.Elapsed += async (_, _) =>
            {
                OnMatchTimerElapsed?.Invoke(SecondsRemainingForMatch);
                CheckForMatchEnd();
            };
            matchEndCheckTimer.AutoReset = true;
            matchEndCheckTimer.Enabled = true;
        }

        private void CheckForMatchEnd()
        {
            if (MatchState != BaseMatchState.MatchActive) { return; }

            if (TimeOfMatchEnd <= DateTime.Now)
            {
                _ = EndMatch(currentLeader);
            }
        }

        /// <summary>
        /// Ends the match, auto-starting the next round if configured.
        /// </summary>
        public async Task EndMatch(ulong? winner)
        {
            if (matchState != BaseMatchState.MatchActive) { return; }

            matchState = BaseMatchState.MatchEnded;
            RaiseMatchStateChange(BaseMatchState.MatchActive, matchState);

            OnMatchEnd?.Invoke(winner);

            matchEndCheckTimer?.Stop();
            matchEndCheckTimer?.Dispose();

            if (autoStartNextMatch)
            {
                await QueueNextMatchStart();
            }
        }

        private async Task QueueNextMatchStart()
        {
            await Task.Delay((int)(autoStartDelaySeconds * 1000));
            await StartMatchCountdown();
        }
        #endregion

        #region Kill Event Handling
        private void OnPlayerKilled(ulong killer, ulong victim)
        {
            if (!IsMatchActive || IsMatchEnded) { return; }

            if (!firstKillHappened)
            {
                firstKillHappened = true;
                OnFirstKill?.Invoke();
            }
        }
        
        private void OnAllStatsUpdated(Dictionary<ulong, PlayerMatchStats> allStats)
        {
            if (!IsMatchActive || IsMatchEnded) { return; }
            if (!firstKillHappened) { return; }

            ulong? newLeader = null;
            float newHighestRating = 0f;

            foreach (var kvp in allStats)
            {
                if (kvp.Value.rating > newHighestRating)
                {
                    newHighestRating = kvp.Value.rating;
                    newLeader = kvp.Key;
                }
            }

            if (newLeader.HasValue)
            {
                if (newHighestRating >= ratingToWin)
                {
                    _ = EndMatch(newLeader);
                }

                if (newLeader != currentLeader)
                {
                    currentLeader = newLeader;
                    highestRating = newHighestRating;
                    OnLeaderChanged?.Invoke(newLeader.Value, newHighestRating);
                }
            }
        }
        #endregion

        #region Leaderboard Queries
        public List<PlayerRanking> GetLeaderboard()
        {
            if (matchStatTracker == null) { return new List<PlayerRanking>(); }

            var allStats = matchStatTracker.GetAllStats();
            var rankings = new List<PlayerRanking>();

            foreach (var kvp in allStats)
            {
                rankings.Add(new PlayerRanking
                {
                    player = kvp.Key,
                    stats = kvp.Value
                });
            }

            rankings = rankings.OrderByDescending(r => r.stats.rating)
                .ThenByDescending(r => r.stats.KDA)
                .ThenBy(r => r.stats.deaths)
                .ToList();

            return rankings;
        }

        public string GetLeaderboardString()
        {
            var leaderboard = GetLeaderboard();
            string result = "=== LEADERBOARD ===\n";

            for (int i = 0; i < leaderboard.Count; i++)
            {
                var ranking = leaderboard[i];
                result += $"#{i + 1} {ranking.player}: {ranking.stats}\n";
            }

            return result;
        }
        #endregion
    }
}
