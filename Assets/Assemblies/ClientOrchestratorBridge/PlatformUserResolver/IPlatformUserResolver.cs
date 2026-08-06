using System.Threading.Tasks;

namespace Assemblies.ClientOrchestratorBridge
{
    public interface IPlatformUserResolver
    {
        public Task<string> GetAuthTicketForIdentityString(string identityString);
    }
}