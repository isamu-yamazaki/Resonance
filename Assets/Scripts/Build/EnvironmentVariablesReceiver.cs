using JetBrains.Annotations;
using UnityEngine;

namespace Resonance.BuildTools
{
    /// <summary>
    /// Detect and receive environment variables related to the game.
    /// Some variables may be unavailable depending on the game context.
    /// </summary>
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

        public ushort? GameServerPort { get; private set; }
        [CanBeNull] public string MatchId { get; private set; }
        [CanBeNull] public string MatchKey { get; private set; }
        [CanBeNull] public string OrchestratorUrl { get; set; }
        [CanBeNull] public string NextSceneName { get; set; }

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
        }
    }
}