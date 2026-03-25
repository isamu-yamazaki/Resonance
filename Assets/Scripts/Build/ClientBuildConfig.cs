using UnityEngine;

namespace Resonance.BuildTools
{
    [CreateAssetMenu(fileName = "ClientBuildConfig", menuName = "Resonance/Client Build Configuration")]
    public class ClientBuildConfig : ScriptableObject
    {
        /// <summary>
        /// When true, activates the Steam lobby provider in the lobby scene.
        /// When false, activates the dummy lobby provider (no Steam required; for local/dev builds).
        /// </summary>
        public bool enableSteamLobby;

        /// <summary>
        /// When true, connects to the remote production relay (PurrRelay).
        /// When false, connects to the local relay - requires PurrLay running on your machine.
        /// </summary>
        public bool useProductionRelay;

        /// <summary>
        /// Note that the game version under the remote orchestrator must match
        /// exactly with the client.
        /// 
        /// Does nothing if not using client-server mode.
        /// </summary>
        public string orchestratorUrl;

        /// <summary>
        /// When true, the client connects to a separate dedicated server.
        /// When false, the client runs as a listen server (host mode).
        /// </summary>
        public bool useClientServerMode;

        /// <summary>
        /// When true, marks this as a production build.
        /// Triggers codesigning and notarization in the post-build step on Mac.
        /// </summary>
        public bool isProduction;
    }

}
