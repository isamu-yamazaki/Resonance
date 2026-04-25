using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Resonance.Assemblies.MatchStat;
using UnityEngine;

namespace Resonance.Assemblies.Arena
{
    public class ArenaRatingManager
    {
        #region Dependencies
        private readonly MatchStatTracker matchStatTracker;
        private readonly ArenaRoundManager arenaRoundManager;
        #endregion

        #region Multipliers and Point Gains
        private readonly List<BaseRatingMultiplier> multipliers = new();
        private readonly List<BasePointGain> pointGains = new();

        // Per-player multiplier instances: playerId -> (multiplierKey -> multiplier instance)
        private readonly Dictionary<ulong, Dictionary<string, BaseRatingMultiplier>> playerMultipliers = new();
        #endregion

        #region Constructor
        public ArenaRatingManager(MatchStatTracker matchStatTracker, ArenaRoundManager arenaRoundManager)
        {
            this.matchStatTracker = matchStatTracker;
            this.arenaRoundManager = arenaRoundManager;

            PopulateMultipliers();
            PopulatePointGains();
            SubscribeToEvents();

#if UNITY_EDITOR
            Debug.Log($"[ArenaRatingManager] Initialized with {multipliers.Count} multiplier type(s) and {pointGains.Count} point gain(s)");
#endif
        }
        #endregion

        #region Reflection Population
        private void PopulateMultipliers()
        {
            var baseType = typeof(BaseRatingMultiplier);
            var concreteTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t));

            foreach (var type in concreteTypes)
            {
                try
                {
                    var instance = (BaseRatingMultiplier)Activator.CreateInstance(
                        type, matchStatTracker, arenaRoundManager);
                    multipliers.Add(instance);
#if UNITY_EDITOR
                    Debug.Log($"[ArenaRatingManager] Registered multiplier: {type.Name} (key: {instance.Key})");
#endif
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ArenaRatingManager] Failed to instantiate multiplier {type.Name}: {e.Message}");
                }
            }
        }

        private void PopulatePointGains()
        {
            var baseType = typeof(BasePointGain);
            var concreteTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t));

            foreach (var type in concreteTypes)
            {
                try
                {
                    var instance = (BasePointGain)Activator.CreateInstance(type, matchStatTracker, arenaRoundManager, this);
                    pointGains.Add(instance);
#if UNITY_EDITOR
                    Debug.Log($"[ArenaRatingManager] Registered point gain: {type.Name}");
#endif
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ArenaRatingManager] Failed to instantiate point gain {type.Name}: {e.Message}");
                }
            }
        }
        #endregion

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            matchStatTracker.OnPlayerKill += HandlePlayerKill;
            matchStatTracker.OnPlayerDeath += HandlePlayerDeath;
            matchStatTracker.OnDamageDealt += HandleDamageDealt;
            matchStatTracker.OnPlayerMiss += HandlePlayerMiss;
            matchStatTracker.OnAllStatsUpdated += HandleAllStatsUpdated;
        }

        public void Unsubscribe()
        {
            matchStatTracker.OnPlayerKill -= HandlePlayerKill;
            matchStatTracker.OnPlayerDeath -= HandlePlayerDeath;
            matchStatTracker.OnDamageDealt -= HandleDamageDealt;
            matchStatTracker.OnPlayerMiss -= HandlePlayerMiss;
            matchStatTracker.OnAllStatsUpdated -= HandleAllStatsUpdated;
        }
        #endregion

        #region Player Registration
        private void HandleAllStatsUpdated(Dictionary<ulong, PlayerMatchStats> allStats)
        {
            foreach (var playerId in allStats.Keys)
            {
                if (!playerMultipliers.ContainsKey(playerId))
                {
                    RegisterPlayer(playerId);
                }
            }
        }

        private void RegisterPlayer(ulong playerId)
        {
            var playerMultiplierMap = new Dictionary<string, BaseRatingMultiplier>();

            foreach (var multiplier in multipliers)
            {
                var instance = (BaseRatingMultiplier)Activator.CreateInstance(
                    multiplier.GetType(), matchStatTracker, arenaRoundManager);
                playerMultiplierMap[instance.Key] = instance;
            }

            playerMultipliers[playerId] = playerMultiplierMap;
#if UNITY_EDITOR
            Debug.Log($"[ArenaRatingManager] Registered player {playerId} with {playerMultiplierMap.Count} multiplier(s)");
#endif
        }
        #endregion

        #region Event Handlers
        private void HandlePlayerKill(ulong killerId, ulong victimId)
        {
            // Multipliers first, then point gains
            foreach (var multiplierMap in playerMultipliers.Values)
                foreach (var multiplier in multiplierMap.Values)
                    multiplier.OnKill(killerId, victimId);

            foreach (var pointGain in pointGains)
                pointGain.OnKill(killerId, victimId);
        }

        private void HandlePlayerDeath(ulong playerId)
        {
            foreach (var multiplierMap in playerMultipliers.Values)
                foreach (var multiplier in multiplierMap.Values)
                    multiplier.OnDeath(playerId);

            foreach (var pointGain in pointGains)
                pointGain.OnDeath(playerId);
        }

        private void HandleDamageDealt(ulong attackerId, ulong victimId, float amount)
        {
            foreach (var multiplierMap in playerMultipliers.Values)
                foreach (var multiplier in multiplierMap.Values)
                    multiplier.OnDamageDealt(attackerId, victimId, amount);

            foreach (var pointGain in pointGains)
                pointGain.OnDamageDealt(attackerId, victimId, amount);
        }

        private void HandlePlayerMiss(ulong playerId)
        {
            foreach (var multiplierMap in playerMultipliers.Values)
                foreach (var multiplier in multiplierMap.Values)
                    multiplier.OnMiss(playerId);

            foreach (var pointGain in pointGains)
                pointGain.OnMiss(playerId);
        }
        #endregion

        #region Multiplier Access
        public float GetMultiplierValue(string key, ulong playerId)
        {
            if (!playerMultipliers.TryGetValue(playerId, out var multiplierMap))
            {
                Debug.LogWarning($"[ArenaRatingManager] No multipliers found for player {playerId}");
                return 1f;
            }

            if (!multiplierMap.TryGetValue(key, out var multiplier))
            {
                Debug.LogWarning($"[ArenaRatingManager] No multiplier found with key '{key}' for player {playerId}");
                return 1f;
            }

            return multiplier.GetValue(playerId);
        }
        #endregion
    }
}