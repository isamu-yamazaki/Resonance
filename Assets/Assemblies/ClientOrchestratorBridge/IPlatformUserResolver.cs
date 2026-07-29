namespace Assemblies.ClientOrchestratorBridge
{
    public interface IPlatformUserResolver
    {
        public string GetPlatformId();
        public string GetAuthTicketForIdentityString(string identityString);
    }
}