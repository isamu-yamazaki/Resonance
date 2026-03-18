using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Assemblies.MatchStat;
using UnityEngine;
using UnityEngine.Events;

namespace Resonance.Match
{
    /// <summary>
    /// NetworkModule adapter that bridges MatchStatTracker with PurrNet networking.
    /// Ensures that stat tracking logic only runs on the server.
    /// </summary>
    [Serializable]
    public class MatchStatNetworkAdapter : NetworkModule
    {
        #region Configuration
        private readonly MatchStatTracker.MatchStatTrackerConfig _config;
        #endregion

        #region Events
        public event Action<Dictionary<PlayerID, PlayerMatchStats>> OnAllStatsUpdate;
        public event Action<PlayerID, PlayerID> OnPlayerKill;
        #endregion

        #region Server State
        private MatchStatTracker _tracker;
        public MatchStatTracker Tracker_Server => _tracker;

        public UnityEvent<MatchStatTracker> OnMatchStatTrackerCreated = new();
        #endregion

        #region Constructor
        public MatchStatNetworkAdapter(MatchStatTracker.MatchStatTrackerConfig config)
        {
            _config = config;
        }
        #endregion

        #region NetworkModule Lifecycle
        public override void OnSpawn(bool asServer)
        {
            base.OnSpawn(asServer);

            if (asServer)
            {
                _tracker = new MatchStatTracker(_config);
                _tracker.OnAllStatsUpdated += OnTrackerStatsUpdated;
                _tracker.OnPlayerKill += OnTrackerPlayerKill;
                OnMatchStatTrackerCreated?.Invoke(_tracker);
            }
        }

        public override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);

            if (asServer && _tracker != null)
            {
                _tracker.OnAllStatsUpdated -= OnTrackerStatsUpdated;
                _tracker.OnPlayerKill -= OnTrackerPlayerKill;
                _tracker = null;
            }
        }
        #endregion

        #region Server Event Handlers
        private void OnTrackerStatsUpdated(Dictionary<ulong, PlayerMatchStats> allStats)
        {
            var playerIdStats = OwnerIDExtractor.UlongDictionaryToPlayerIDDictionary(allStats);
            FirePlayerStatObservers(playerIdStats);
        }

        private void OnTrackerPlayerKill(ulong killer, ulong victim)
        {
            FireOnKillObservers(killer, victim);
        }
        #endregion

        #region Server to Client RPCs
        [ObserversRpc]
        private void FirePlayerStatObservers(Dictionary<PlayerID, PlayerMatchStats> allStats)
        {
            Debug.Log($"[MatchStatNetworkAdapter] Stats update received for {allStats.Count} players");
            OnAllStatsUpdate?.Invoke(allStats);
        }

        [ObserversRpc]
        private void FireOnKillObservers(ulong killer, ulong victim)
        {
            OnPlayerKill?.Invoke(
                OwnerIDExtractor.UlongToPlayerId(killer),
                OwnerIDExtractor.UlongToPlayerId(victim)
            );
        }
        #endregion

        #region Client to Server Actions (Public API)
        public void RecordKill(GameObject killer, GameObject victim)
        {
            if (OwnerIDExtractor.TryExtractPlayerIds(killer, victim, out ulong killerId, out ulong victimId))
            {
                Debug.Log($"[MatchStatNetworkAdapter] Logging kill: killer={killerId}, victim={victimId}");
                RecordKill_Server(killerId, victimId);
            }
        }

        [ServerRpc]
        private void RecordKill_Server(ulong killer, ulong victim)
        {
            _tracker?.RecordKill(killer, victim);
        }

        public void RecordDamage(GameObject attacker, GameObject victim, float amount)
        {
            if (OwnerIDExtractor.TryExtractPlayerIds(attacker, victim, out ulong attackerId, out ulong victimId))
            {
                Debug.Log($"[MatchStatNetworkAdapter] Logging damage: attacker={attackerId}, victim={victimId}, amount={amount}");
                RecordDamage_Server(attackerId, victimId, amount);
            }
        }

        [ServerRpc]
        private void RecordDamage_Server(ulong attacker, ulong victim, float amount)
        {
            _tracker?.RecordDamage(attacker, victim, amount);
        }

        public void RecordDeath(GameObject victim)
        {
            if (OwnerIDExtractor.TryExtractPlayerIds(victim, out ulong id))
            {
                Debug.Log($"[MatchStatNetworkAdapter] Logging death: id={id}");
                RecordDeath_Server(id);
            }
        }

        [ServerRpc]
        private void RecordDeath_Server(ulong idPrimitive)
        {
            _tracker?.RecordDeath(idPrimitive);
        }

        public void RegisterPlayer(GameObject player)
        {
            if (OwnerIDExtractor.TryExtractPlayerIds(player, out ulong id))
            {
                Debug.Log($"[MatchStatNetworkAdapter] Registering player: id={id}");
                RegisterPlayer_Server(id);
            }
        }

        [ServerRpc]
        private void RegisterPlayer_Server(ulong id)
        {
            _tracker?.RegisterPlayer(id);
        }

        public void UnregisterPlayer(GameObject player)
        {
            if (OwnerIDExtractor.TryExtractPlayerIds(player, out ulong id))
            {
                Debug.Log($"[MatchStatNetworkAdapter] Unregistering player: id={id}");
                UnregisterPlayer_Server(id);
            }
        }

        [ServerRpc]
        private void UnregisterPlayer_Server(ulong id)
        {
            _tracker?.UnregisterPlayer(id);
        }

        [ServerRpc]
        public void ResetAllStats()
        {
            Debug.Log("[MatchStatNetworkAdapter] Resetting all stats");
            _tracker?.ResetAllStats();
        }
        #endregion

        #region Getters (Client Callable)
        public async Task<PlayerMatchStats?> GetStats(GameObject player)
        {
            if (OwnerIDExtractor.TryExtractPlayerIds(player, out ulong playerId))
            {
                return await GetStats(playerId);
            }
            return null;
        }

        public async Task<PlayerMatchStats?> GetStats(PlayerID player)
        {
            return await GetStats(player.id.value);
        }

        [ServerRpc]
        public async Task<PlayerMatchStats> GetStats(ulong playerId)
        {
            return _tracker.GetStats(playerId);
        }

        [ServerRpc]
        public async Task<Dictionary<PlayerID, PlayerMatchStats>> GetAllStats()
        {
            return OwnerIDExtractor.UlongDictionaryToPlayerIDDictionary(_tracker.GetAllStats());
        }
        #endregion

    }
}
