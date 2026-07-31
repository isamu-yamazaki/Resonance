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
    }
}
