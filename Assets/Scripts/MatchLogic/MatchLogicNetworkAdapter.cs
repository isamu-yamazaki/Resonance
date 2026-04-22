using System;
using PurrNet;
using Resonance.Assemblies.Arena;
using Resonance.Assemblies.LobbySystem;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.Polarity;
using Resonance.GameBootstrap;
using UnityEngine;

namespace Resonance.Match
{
    /// <summary>
    /// Central NetworkBehaviour that hosts all match-related NetworkModules.
    /// Provides singleton access to submodules for match statistics and game mode logic.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(GameModeProvider))]
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
        [SerializeField] private float ratingToWin = 2000f;
        [SerializeField] private float autoStartDelaySeconds = 5f;
        [SerializeField] private bool autoStartNextMatch = false;
        [SerializeField] private float matchDurationSeconds = 300f;

        [Header("Arena Short Settings")]
        [SerializeField] private float arenaShortMatchDurationSeconds = 150f;


        [Header("Polarity Settings")]
        [SerializeField] private int teamEliminationsToWin = 10;
        [SerializeField] private int timeBetweenRoleSwitchSeconds = 90;
        #endregion

        #region Modules
        private MatchStatNetworkAdapter _matchStatAdapter;

        /// <summary>
        /// Returns a transient reference to the match stats network module.
        /// Do NOT store the returned reference in a field, especially on a NetworkBehaviour or NetworkModule:
        /// PurrNet's codegen scans fields (including auto-property backing fields) on those types and
        /// re-registers the module under the storing parent.
        /// </summary>
        public MatchStatNetworkAdapter GetTemporaryMatchStatsReference() => _matchStatAdapter;

        private BaseRoundManagerNetworkAdapter currentRoundManagerNetworkAdapter;

        /// <summary>
        /// Returns a transient reference to the active round manager network module.
        /// Do NOT store the returned reference in a field, especially on a NetworkBehaviour or NetworkModule:
        /// PurrNet's codegen scans fields (including auto-property backing fields) on those types and
        /// re-registers the module under the storing parent.
        /// </summary>
        public BaseRoundManagerNetworkAdapter GetTemporaryActiveRoundManagerReference() => currentRoundManagerNetworkAdapter;
        #endregion

        #region Events
        public event Action OnFinishedConfiguring;
        public bool HasFinishedConfiguring { get; private set; }
        #endregion

        #region Lifecycle
        private void Awake()
        {
            if (InstanceHandler.TryGetInstance<MatchLogicNetworkAdapter>(out var _))
            {
                Destroy(this);
                return;
            }

            InstanceHandler.RegisterInstance(this);
        }

        protected override void OnDestroy()
        {
            if (InstanceHandler.TryGetInstance<MatchLogicNetworkAdapter>(out var instance) && instance == this)
            {
                InstanceHandler.UnregisterInstance<MatchLogicNetworkAdapter>();
            }
        }

        // See https://purrnet.gitbook.io/docs/systems-and-modules/network-modules/common-pitfalls
        protected override void OnInitializeModules()
        {
            base.OnInitializeModules();

            var gameModeProvider = FindFirstObjectByType<GameModeProvider>();
            Configure(gameModeProvider.gameMode);
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
                    ratingToWin = ratingToWin,
                    autoStartNextMatch = autoStartNextMatch,
                    autoStartDelaySeconds = autoStartDelaySeconds,
                    matchStartCountdownSeconds = matchStartCountdownSeconds,
                    matchDurationSeconds = matchDurationSeconds,
                };
                currentRoundManagerNetworkAdapter = new ArenaRoundManagerNetworkAdapter(_matchStatAdapter, arenaConfig);
            }
            else if (gameMode == GameMode.Polarity)
            {
                var polarityConfig = new PolarityRoundManager.PolarityRoundManagerConfig
                {
                    teamEliminationsToWin = teamEliminationsToWin,
                    timeBetweenRoleSwitchSeconds = timeBetweenRoleSwitchSeconds,
                    matchStartCountdownSeconds = matchStartCountdownSeconds,
                };
                currentRoundManagerNetworkAdapter = new PolarityRoundManagerNetworkAdapter(_matchStatAdapter, polarityConfig);
            }
            else if (gameMode == GameMode.ArenaShort)
            {
                var arenaConfig = new ArenaRoundManager.ArenaRoundManagerConfig
                {
                    ratingToWin = ratingToWin,
                    autoStartNextMatch = autoStartNextMatch,
                    autoStartDelaySeconds = autoStartDelaySeconds,
                    matchStartCountdownSeconds = matchStartCountdownSeconds,
                    matchDurationSeconds = arenaShortMatchDurationSeconds,
                };
                currentRoundManagerNetworkAdapter = new ArenaRoundManagerNetworkAdapter(_matchStatAdapter, arenaConfig);
            }

            OnFinishedConfiguring?.Invoke();
            HasFinishedConfiguring = true;
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
