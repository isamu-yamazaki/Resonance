using UnityEngine;
using UnityEngine.Serialization;

namespace Resonance.BuildTools
{
    /// <summary>
    /// Build configuration for dedicated server builds.
    /// </summary>
    [CreateAssetMenu(fileName = "ServerBuildConfig", menuName = "Resonance/Server Build Configuration")]
    public class ServerBuildConfig : ScriptableObject
    {
        /// <summary>
        /// Determine whether to create a development or production build.
        /// </summary>
        [FormerlySerializedAs("useProductionRelay")] public bool isProductionBuild;

        /// <summary>
        /// A string that all clients must agree on when connecting to the server orchestrator,
        /// before connecting to a server build with this value.
        /// BuildScript.cs overwrites this value when making a build.
        /// The value set in the editor is for use within the editor only.
        /// </summary>
        public string intendedServerVersion;
    }
}
