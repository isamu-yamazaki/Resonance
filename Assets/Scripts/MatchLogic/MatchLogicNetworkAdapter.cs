using PurrNet;
using Resonance.Assemblies.Arena;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.Polarity;
using Resonance.GameBootstrap;
using Resonance.LobbySystem;
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

        [Header("General Gameplay Settings")]
        [SerializeField] private float matchStartCountdownSeconds = 5f;

        [Header("Arena Settings")]
        [SerializeField] private int eliminationsToWin = 10;
        [SerializeField] private float autoStartDelaySeconds = 5f;
        [SerializeField] private bool autoStartNextMatch = false;
        [SerializeField] private float matchDurationSeconds = 300f;


        [Header("Polarity Settings")]
        [SerializeField] private int teamEliminationsToWin = 10;
        [SerializeField] private int timeBetweenRoleSwitchSeconds = 90;
        #endregion

        #region Modules
        private MatchStatNetworkAdapter _matchStatAdapter;
        public MatchStatNetworkAdapter MatchStats => _matchStatAdapter;

        private BaseRoundManagerNetworkAdapter currentRoundManagerNetworkAdapter;
        public BaseRoundManagerNetworkAdapter ActiveRoundManager => currentRoundManagerNetworkAdapter;
        #endregion

        #region Lifecycle
        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
            DontDestroyOnLoad(this);

            var gameModeProvider = FindFirstObjectByType<GameModeProvider>();
            Configure(gameModeProvider.gameMode);
        }

        private void OnDestroy()
        {
            InstanceHandler.UnregisterInstance<MatchLogicNetworkAdapter>();
        }
        #endregion

        #region Setup
        private void Configure(GameMode gameMode)
        {
            var matchStatConfig = new MatchStatTracker.MatchStatTrackerConfig
            {
                assistTimeWindowMs = assistTimeWindow,
                assistDamageThreshold = assistDamageThreshold
            };
            _matchStatAdapter = new MatchStatNetworkAdapter(matchStatConfig);

            if (gameMode == GameMode.Arena)
            {
                var arenaConfig = new ArenaRoundManager.ArenaRoundManagerConfig
                {
                    eliminationsToWin = eliminationsToWin,
                    autoStartNextMatch = autoStartNextMatch,
                    autoStartDelaySeconds = autoStartDelaySeconds,
                    matchStartCountdownSeconds = matchStartCountdownSeconds,
                    matchDurationSeconds = matchDurationSeconds,
                };
                currentRoundManagerNetworkAdapter = new ArenaRoundManagerNetworkAdapter(_matchStatAdapter, arenaConfig);
            } else if (gameMode == GameMode.Polarity)
            {
                var polarityConfig = new PolarityRoundManager.PolarityRoundManagerConfig
                {
                    teamEliminationsToWin = teamEliminationsToWin,
                    timeBetweenRoleSwitchSeconds = timeBetweenRoleSwitchSeconds,
                    matchStartCountdownSeconds = matchStartCountdownSeconds,
                };
                currentRoundManagerNetworkAdapter = new PolarityRoundManagerNetworkAdapter(_matchStatAdapter, polarityConfig);
            }
        }

        #endregion

        #region Debugging
        [ContextMenu("Log match active status")]
        public async void LogIsMatchActive()
        {
            var activeStatus = await currentRoundManagerNetworkAdapter.GetIsMatchActive();
            Debug.Log($"Is match active: {activeStatus}");
        }

        #endregion
    }
}
