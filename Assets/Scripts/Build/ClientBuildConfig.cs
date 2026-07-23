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
        /// Note that the game version under the remote orchestrator must match
        /// exactly with the client.
        /// </summary>
        public string orchestratorUrl;

        /// <summary>
        /// When true, marks this as a production build.
        /// Triggers codesigning and notarization in the post-build step on Mac.
        /// When false, marks this as a development build through Unity.
        /// </summary>
        public bool isProduction;
    }

}
