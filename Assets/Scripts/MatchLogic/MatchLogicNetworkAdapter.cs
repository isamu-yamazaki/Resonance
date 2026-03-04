using PurrNet;
using Resonance.Assemblies.Arena;
using Resonance.Assemblies.MatchStat;
using UnityEngine;

namespace Resonance.Match
{
    /// <summary>
    /// Central NetworkBehaviour that hosts all match-related NetworkModules.
    /// Provides singleton access to submodules for match statistics and game mode logic.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class MatchLogicNetworkAdapter : NetworkBehaviour
    {
        public static MatchLogicNetworkAdapter Instance
        {
            get
            {
                if (InstanceHandler.TryGetInstance<MatchLogicNetworkAdapter>(out var instance))
                {
                    return instance;
                }
                return null;
            }
        }

        #region Inspector Fields
        [Header("Match Stats Settings")]
        [SerializeField] private float assistTimeWindow = 5f;
        [SerializeField] private float assistDamageThreshold = 20f;

        [Header("Arena Settings")]
        [SerializeField] private int eliminationsToWin = 10;
        [SerializeField] private float autoStartDelaySeconds = 5f;
        [SerializeField] private bool autoStartNextMatch = false;
        [SerializeField] private float matchStartCountdownSeconds = 5f;
        [SerializeField] private float matchDurationSeconds = 300f;
        #endregion

        #region Modules
        private MatchStatNetworkAdapter _matchStatAdapter;
        public MatchStatNetworkAdapter MatchStats => _matchStatAdapter;

        private ArenaRoundManagerNetworkAdapter _arenaRoundManagerNetworkAdapter;
        public ArenaRoundManagerNetworkAdapter ArenaRoundManager => _arenaRoundManagerNetworkAdapter;
        #endregion

        #region Lifecycle
        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);

            var matchStatConfig = new MatchStatTracker.MatchStatTrackerConfig
            {
                assistTimeWindowMs = assistTimeWindow,
                assistDamageThreshold = assistDamageThreshold
            };
            _matchStatAdapter = new MatchStatNetworkAdapter(matchStatConfig);

            var arenaConfig = new ArenaRoundManager.ArenaRoundManagerConfig
            {
                eliminationsToWin = eliminationsToWin,
                autoStartNextMatch = autoStartNextMatch,
                autoStartDelaySeconds = autoStartDelaySeconds,
                matchStartCountdownSeconds = matchStartCountdownSeconds,
                matchDurationSeconds = matchDurationSeconds,
            };
            _arenaRoundManagerNetworkAdapter = new ArenaRoundManagerNetworkAdapter(_matchStatAdapter, arenaConfig);

            DontDestroyOnLoad(this);
        }

        private void OnDestroy()
        {
            InstanceHandler.UnregisterInstance<MatchLogicNetworkAdapter>();
        }
        #endregion

        #region Debugging
        [ContextMenu("Log match active status")]
        public async void LogIsMatchActive()
        {
            var activeStatus = await _arenaRoundManagerNetworkAdapter.GetIsMatchActive();
            Debug.Log($"Is match active: {activeStatus}");
        }

        #endregion
    }
}
