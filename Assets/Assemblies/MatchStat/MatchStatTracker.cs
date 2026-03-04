using System;
using System.Collections.Generic;

namespace Resonance.Assemblies.MatchStat
{
    public class MatchStatTracker
    {
        #region Configuration
        public struct MatchStatTrackerConfig
        {
            public float assistTimeWindowMs;
            public float assistDamageThreshold;

            public static MatchStatTrackerConfig Default => new()
            {
                assistTimeWindowMs = 5f,
                assistDamageThreshold = 20f
            };
        }
        #endregion

        #region Player Stats Data
        private Dictionary<ulong, PlayerMatchStats> playerStats = new();
        private AssistCalculator assistCalculator;
        #endregion

        #region Events
        public event Action<Dictionary<ulong, PlayerMatchStats>> OnAllStatsUpdated;
        public event Action<ulong, PlayerMatchStats> OnStatsUpdated;
        public event Action<ulong, ulong> OnPlayerKill; // (killer, victim)
        public event Action<ulong, ulong> OnPlayerAssist; // (assister, victim)
        #endregion

        #region Startup
        public MatchStatTracker() : this(MatchStatTrackerConfig.Default)
        {
        }

        public MatchStatTracker(MatchStatTrackerConfig config)
        {
            assistCalculator = new AssistCalculator(config.assistTimeWindowMs, config.assistDamageThreshold);
        }
        #endregion

        #region Player Registration
        public void RegisterPlayer(ulong playerId)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                playerStats[playerId] = new PlayerMatchStats();
            }
        }

        public void UnregisterPlayer(ulong playerId)
        {
            playerStats.Remove(playerId);
        }
        #endregion

        #region Damage Tracking
        public void RecordDamage(ulong attackerId, ulong victimId, float damageAmount)
        {
            RegisterPlayer(attackerId);
            RegisterPlayer(victimId);

            playerStats[attackerId] = playerStats[attackerId].RecordDamage(damageAmount);

            assistCalculator.RecordDamage(attackerId, victimId, damageAmount);
        }
        #endregion

        #region Kill/Death/Assist Recording
        public void RecordKill(ulong killerId, ulong victimId)
        {
            if (killerId == 0 || victimId == 0) return;

            RegisterPlayer(killerId);
            RegisterPlayer(victimId);

            playerStats[killerId] = playerStats[killerId].RecordKill();
            playerStats[victimId] = playerStats[victimId].RecordDeath();

            OnPlayerKill?.Invoke(killerId, victimId);

            // Process assists
            ProcessAssists(killerId, victimId);

            // Clear damage contributions for victim
            assistCalculator.ClearDamageForVictim(victimId);

            // Notify stats updated
            OnStatsUpdated?.Invoke(killerId, playerStats[killerId]);
            OnStatsUpdated?.Invoke(victimId, playerStats[victimId]);
            OnAllStatsUpdated?.Invoke(playerStats);
        }

        private void ProcessAssists(ulong killerId, ulong victimId)
        {
            var assisters = assistCalculator.GetAssistAttackersForVictim(victimId, killerId);

            foreach (var assisterId in assisters)
            {
                RegisterPlayer(assisterId);
                playerStats[assisterId] = playerStats[assisterId].RecordAssist();

                OnPlayerAssist?.Invoke(assisterId, victimId);
                OnStatsUpdated?.Invoke(assisterId, playerStats[assisterId]);
                OnAllStatsUpdated?.Invoke(playerStats);
            }
        }

        public void RecordDeath(ulong victimId)
        {
            RegisterPlayer(victimId);
            playerStats[victimId] = playerStats[victimId].RecordDeath();

            OnStatsUpdated?.Invoke(victimId, playerStats[victimId]);
            OnAllStatsUpdated?.Invoke(playerStats);
        }
        #endregion

        #region Stats Retrieval
        public PlayerMatchStats GetStats(ulong playerId)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                RegisterPlayer(playerId);
            }
            return playerStats[playerId];
        }

        public float GetKDA(ulong playerId)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                return 0f;
            }

            PlayerMatchStats stats = playerStats[playerId];

            // KDA = (Kills + Assists) / Deaths
            // If deaths is 0, just return kills + assists
            if (stats.deaths == 0)
            {
                return stats.kills + stats.assists;
            }

            return (float)(stats.kills + stats.assists) / stats.deaths;
        }

        public Dictionary<ulong, PlayerMatchStats> GetAllStats()
        {
            return new Dictionary<ulong, PlayerMatchStats>(playerStats);
        }
        #endregion

        #region Match Management
        public void ResetAllStats()
        {
            // Create a list of keys to avoid collection modification during enumeration
            var playerIds = new List<ulong>(playerStats.Keys);

            foreach (var playerId in playerIds)
            {
                playerStats[playerId] = new PlayerMatchStats();
            }

            assistCalculator.Clear();

            // Fire event once after all stats are reset
            OnAllStatsUpdated?.Invoke(playerStats);
        }

        public void ResetPlayerStats(ulong playerId)
        {
            if (playerStats.ContainsKey(playerId))
            {
                playerStats[playerId] = new PlayerMatchStats();
                OnAllStatsUpdated?.Invoke(playerStats);
            }
        }
        #endregion
    }
}
