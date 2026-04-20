namespace Resonance.Match
{
    /// <summary>
    /// Static facade for accessing ArenaRoundManagerNetworkAdapter via the MatchLogicNetworkAdapter singleton.
    /// Returns null if the active round manager is not an ArenaRoundManagerNetworkAdapter.
    /// </summary>
    public static class ArenaRoundManagerBridge
    {
        /// <summary>
        /// Returns a transient reference to the active Arena round manager network module.
        /// Do NOT store the returned reference in a field on a NetworkBehaviour or NetworkModule;
        /// PurrNet's codegen will re-register the module under the storing parent and cause undefined behavior.
        /// </summary>
        public static ArenaRoundManagerNetworkAdapter GetTemporaryReference() =>
            MatchLogicNetworkAdapter.Instance?.GetTemporaryActiveRoundManagerReference()
                as ArenaRoundManagerNetworkAdapter;
    }
}
