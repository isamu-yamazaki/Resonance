using System.Threading.Tasks;

namespace Assemblies.ClientOrchestratorBridge
{
    public interface IPlatformUserResolver
    {
        public string GetPlatformId();
        public Task<string> GetAuthTicketForIdentityString(string identityString);
    }
}