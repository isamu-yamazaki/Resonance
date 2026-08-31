using JetBrains.Annotations;
using UnityEngine;

namespace Resonance.BuildTools
{
    /// <summary>
    /// Detect and receive environment variables related to the game.
    /// Some variables may be unavailable depending on the game context.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class EnvironmentVariablesReceiver : MonoBehaviour
    {
        public static EnvironmentVariablesReceiver Instance { get; private set; }

        /// <summary>
        /// The port of the game server to use.
        /// Matches Edgegap's port variable in all environments.
        /// May be read automatically by PurrNet in some cases.
        /// </summary>
        private const string GameServerPortVariable = "ARBITRIUM_PORT_GAMEPORT_INTERNAL";

        private const string MatchIdVariable = "RESONANCE_MATCH_ID";
        private const string MatchKeyVariable = "RESONANCE_MATCH_KEY";
        private const string OrchestratorUrlVariable = "RESONANCE_ORCHESTRATOR_URL";

        private const string NextSceneVariable = "RESONANCE_NEXT_SCENE_NAME";
        private const string GameModeVariable = "RESONANCE_GAME_MODE";
        private const string IntendedServerVersionVariable = "RESONANCE_INTENDED_SERVER_VERSION";

        public ushort? GameServerPort { get; private set; }
        [CanBeNull] public string MatchId { get; private set; }
        [CanBeNull] public string MatchKey { get; private set; }
        [CanBeNull] public string OrchestratorUrl { get; private set; }
        [CanBeNull] public string NextSceneName { get; private set; }
        [CanBeNull] public string GameMode { get; private set; }
        [CanBeNull] public string IntendedServerVersion { get; private set; }

        public bool AllVariablesSet => GameServerPort.HasValue && MatchId != null && MatchKey != null &&
                                       OrchestratorUrl != null && NextSceneName != null && GameMode != null;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            var gameServerPort = System.Environment.GetEnvironmentVariable(GameServerPortVariable);
            if (ushort.TryParse(gameServerPort, out var port))
            {
                GameServerPort = port;
            }

            MatchId = System.Environment.GetEnvironmentVariable(MatchIdVariable);
            MatchKey = System.Environment.GetEnvironmentVariable(MatchKeyVariable);
            OrchestratorUrl = System.Environment.GetEnvironmentVariable(OrchestratorUrlVariable);
            NextSceneName = System.Environment.GetEnvironmentVariable(NextSceneVariable);
            GameMode = System.Environment.GetEnvironmentVariable(GameModeVariable);
            IntendedServerVersion = System.Environment.GetEnvironmentVariable(IntendedServerVersionVariable);
        }

#if UNITY_EDITOR
        public void SetVariables(
            ushort? gameServerPort,
            string matchId,
            string matchKey,
            string orchestratorUrl,
            string nextSceneName,
            string gameMode,
            string intendedServerVersion
        )
        {
            GameServerPort = gameServerPort;
            MatchId = matchId;
            MatchKey = matchKey;
            OrchestratorUrl = orchestratorUrl;
            NextSceneName = nextSceneName;
            GameMode = gameMode;
            IntendedServerVersion = intendedServerVersion;
        }
#endif
    }
}