using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena
{
    public class ArenaRoundManager
    {
        #region Configuration
        public struct ArenaRoundManagerConfig
        {
            public int eliminationsToWin;
            public bool autoStartNextMatch;
            public float autoStartDelaySeconds;
            public float matchStartCountdownSeconds;
            public float matchDurationSeconds;

            public static ArenaRoundManagerConfig Default => new()
            {
                eliminationsToWin = 10,
                autoStartNextMatch = false,
                autoStartDelaySeconds = 5f,
                matchStartCountdownSeconds = 5f,
                matchDurationSeconds = 300f,
            };
        }
        private int eliminationsToWin = 10;
        private float autoStartDelaySeconds = 3f;
        private float matchStartCountdownSeconds = 5f;
        private bool autoStartNextMatch = false;
        private float matchDurationSeconds = 300f;
        #endregion

        #region State
        private ArenaMatchState matchState = ArenaMatchState.Waiting;
        private ulong? currentLeader = null;
        private int highestEliminations = 0;
        private DateTime timeOfLastMatchStart = default;
        private Timer matchEndCheckTimer = null;
        #endregion

        #region Events
        public event Action<ArenaMatchState, ArenaMatchState> OnMatchStateChange;  // Old state, new state
        public event Action<float> OnMatchCountdownStart;  // Seconds
        public event Action OnMatchStart;
        public event Action<ulong?> OnMatchEnd; // Winner
        public event Action<ulong, int> OnLeaderChanged; // New leader, their eliminations
        public event Action<double> OnMatchTimerElapsed; // Total seconds remaining
        #endregion

        #region Properties
        public ArenaMatchState MatchState => matchState;
        public bool IsMatchActive => matchState == ArenaMatchState.MatchActive;
        public bool IsMatchEnded => matchState == ArenaMatchState.MatchEnded;
        public int EliminationsToWin => eliminationsToWin;
        public ulong? CurrentLeader => currentLeader;
        public int HighestEliminations => highestEliminations;
        public float MatchStartCountdownSeconds => matchStartCountdownSeconds;
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

        private MatchStatTracker matchStatTracker;

        public ArenaRoundManager(MatchStatTracker tracker)
            : this(tracker, ArenaRoundManagerConfig.Default)
        {
        }

        public ArenaRoundManager(MatchStatTracker tracker, ArenaRoundManagerConfig config)
        {
            matchStatTracker = tracker;
            autoStartNextMatch = config.autoStartNextMatch;
            eliminationsToWin = config.eliminationsToWin;
            autoStartDelaySeconds = config.autoStartDelaySeconds;
            matchStartCountdownSeconds = config.matchStartCountdownSeconds;
            matchDurationSeconds = config.matchDurationSeconds;

            SubscribeToEvents();
        }

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            if (matchStatTracker != null)
            {
                matchStatTracker.OnPlayerKill += OnPlayerKilled;
            }
        }
        #endregion

        #region Match Control
        public async Task StartMatchCountdown()
        {
            if (matchState == ArenaMatchState.MatchActive || matchState == ArenaMatchState.Countdown)
            {
                return;
            }

            var oldMatchState = matchState;

            matchState = ArenaMatchState.Countdown;

            OnMatchCountdownStart?.Invoke(matchStartCountdownSeconds);
            OnMatchStateChange?.Invoke(oldMatchState, matchState);

            await Task.Delay((int)(matchStartCountdownSeconds * 1000));
            StartMatchWithoutCountdown();
        }

        public void StartMatchWithoutCountdown()
        {
            if (IsMatchActive)
            {
                return;
            }

            var oldMatchState = matchState;
            matchState = ArenaMatchState.MatchActive;

            currentLeader = null;
            highestEliminations = 0;
            timeOfLastMatchStart = DateTime.Now;

            matchStatTracker?.ResetAllStats();

            OnMatchStart?.Invoke();
            OnMatchStateChange?.Invoke(oldMatchState, matchState);

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
            if (MatchState != ArenaMatchState.MatchActive)
            {
                return;
            }

            if (TimeOfMatchEnd <= DateTime.Now)
            {
                _ = EndMatch(currentLeader);
            }
        }

        /// <summary>
        /// Ends the match, auto-starting the next round if configured.
        /// </summary>
        /// <param name="winner"></param>
        /// <returns></returns>
        public async Task EndMatch(ulong? winner)
        {
            if (matchState != ArenaMatchState.MatchActive) return;

            matchState = ArenaMatchState.MatchEnded;
            OnMatchStateChange?.Invoke(ArenaMatchState.MatchActive, matchState);

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
            if (!IsMatchActive || IsMatchEnded) return;

            PlayerMatchStats? killerStats = matchStatTracker.GetStats(killer);
            if (killerStats is PlayerMatchStats stats)
            {
                ConditionallyUpdateLeader(killer, stats);

                int currentEliminations = stats.kills;
                if (currentEliminations >= eliminationsToWin)
                {
                    _ = EndMatch(killer);
                }
            }
        }

        private int ConditionallyUpdateLeader(ulong killer, PlayerMatchStats stats)
        {
            int currentEliminations = stats.kills;

            // Update leader tracking
            if (currentEliminations > highestEliminations)
            {
                highestEliminations = currentEliminations;
                var previousLeader = currentLeader;
                currentLeader = killer;

                if (previousLeader != killer)
                {
                    OnLeaderChanged?.Invoke(killer, currentEliminations);
                }
            }

            return currentEliminations;
        }
        #endregion

        #region Leaderboard Queries
        public List<PlayerRanking> GetLeaderboard()
        {
            if (matchStatTracker == null) return new List<PlayerRanking>();

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

            // Sort by kills (descending), then by KDA (descending), then by deaths (ascending)
            rankings = rankings.OrderByDescending(r => r.stats.kills)
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
