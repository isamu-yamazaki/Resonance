using UnityEngine;

namespace Resonance.BuildTools
{
    /// <summary>
    /// Build configuration for dedicated server builds.
    /// </summary>
    [CreateAssetMenu(fileName = "ServerBuildConfig", menuName = "Resonance/Server Build Configuration")]
    public class ServerBuildConfig : ScriptableObject
    {
        /// <summary>
        /// When true, connects to the remote production relay (PurrRelay).
        /// When false, connects to the local relay — requires PurrLay running on your machine.
        /// </summary>
        public bool useProductionRelay;
    }
}
