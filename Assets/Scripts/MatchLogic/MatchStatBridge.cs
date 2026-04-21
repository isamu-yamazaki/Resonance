namespace Resonance.Match
{
    /// <summary>
    /// Static facade for accessing MatchStatNetworkAdapter via the MatchLogicNetworkAdapter singleton.
    /// </summary>
    public static class MatchStatBridge
    {
        /// <summary>
        /// Returns a transient reference to the match stats network module.
        /// Do NOT store the returned reference in a field on a NetworkBehaviour or NetworkModule;
        /// PurrNet's codegen will re-register the module under the storing parent and cause undefined behavior.
        /// </summary>
        public static MatchStatNetworkAdapter GetTemporaryReference() =>
            MatchLogicNetworkAdapter.Instance?.GetTemporaryMatchStatsReference();
    }
}
