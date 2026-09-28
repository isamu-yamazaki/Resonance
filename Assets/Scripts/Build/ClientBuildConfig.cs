using UnityEngine;
using UnityEngine.Serialization;

namespace Resonance.BuildTools
{
    [CreateAssetMenu(fileName = "ClientBuildConfig", menuName = "Resonance/Client Build Configuration")]
    public class ClientBuildConfig : ScriptableObject
    {
        /// <summary>
        /// When true, activates the Steam lobby provider in the lobby scene.
        /// When false, activates the dummy lobby provider.
        /// </summary>
        public bool enableSteamLobby;

        /// <summary>
        /// The full base URL for the orchestrator (e.g. https://example.com).
        /// For local testing, use http://127.0.0.1:9000 or a different port number.
        /// </summary>
        public string orchestratorUrl;

        /// <summary>
        /// When true, triggers codesigning and notarization in the post-build step on Mac.
        /// </summary>
        [FormerlySerializedAs("isProduction")] public bool useCodesigningAndNotarizationOnMac;

        /// <summary>
        /// When true, adds BuildOptions.Development to the BuildPlayerOptions.
        /// </summary>
        public bool unityDevelopmentBuild;

        /// <summary>
        /// When true, copies steam_appid.txt next to the executable.
        /// Builds submitted to Steam should *not* have this option checked!
        /// This option is only for testing Steam integration in a build outside of Steam.
        /// </summary>
        public bool copySteamAppId;

        /// <summary>
        /// A string to pass to the orchestrator for it to find and validate the correct server version.
        /// BuildScript.cs overwrites this value when making a build.
        /// The value set in the editor is for use within the editor only.
        /// </summary>
        public string intendedServerVersion;
    }
}