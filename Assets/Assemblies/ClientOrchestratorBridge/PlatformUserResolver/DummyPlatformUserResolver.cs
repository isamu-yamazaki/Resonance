using System;
using System.Threading.Tasks;

namespace Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// Use for development only. Stands in for a real platform (e.g. Steam) when the game runs
    /// against the dummy lobby provider, where there is no platform SDK to resolve an identity
    /// from and no ticket for the orchestrator to validate.
    /// </summary>
    public class DummyPlatformUserResolver : IPlatformUserResolver
    {
        private readonly string _platformId;

        public DummyPlatformUserResolver(string platformId)
        {
            _platformId = platformId;
        }

        public DummyPlatformUserResolver() : this(Guid.NewGuid().ToString())
        {
        }

        public string GetPlatformId()
        {
            return _platformId;
        }

        public Task<string> GetAuthTicketForIdentityString(string identityString)
        {
            return Task.FromResult(string.Empty);
        }
    }
}
