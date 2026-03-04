namespace Resonance.Match
{
    /// <summary>
    /// Backward compatibility facade for MatchStatNetworkAdapter.
    /// New code should use NetworkingBridge.Instance.MatchStats directly.
    /// </summary>
    public static class MatchStatBridge
    {
        public static MatchStatNetworkAdapter Instance => MatchLogicNetworkAdapter.Instance?.MatchStats;
    }
}
