namespace Resonance.Match
{
    /// <summary>
    /// Static facade for accessing PolarityRoundManagerNetworkAdapter via the MatchLogicNetworkAdapter singleton.
    /// Returns null if the active round manager is not a PolarityRoundManagerNetworkAdapter.
    /// </summary>
    public static class PolarityRoundManagerBridge
    {
        /// <summary>
        /// Returns a transient reference to the active Polarity round manager network module.
        /// Do NOT store the returned reference in a field on a NetworkBehaviour or NetworkModule;
        /// PurrNet's codegen will re-register the module under the storing parent and cause undefined behavior.
        /// </summary>
        public static PolarityRoundManagerNetworkAdapter GetTemporaryReference() =>
            MatchLogicNetworkAdapter.Instance?.GetTemporaryActiveRoundManagerReference()
                as PolarityRoundManagerNetworkAdapter;
    }
}
