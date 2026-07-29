using Assemblies.ClientOrchestratorBridge;

public class FakeUserResolver : IPlatformUserResolver
{
    private readonly string _platformId;
    private readonly string _authTicket;

    public FakeUserResolver(
        string platformId,
        string authTicket
    )
    {
        _platformId = platformId;
        _authTicket = authTicket;
    }

    public string GetPlatformId()
    {
        return _platformId;
    }

    public string GetAuthTicketForIdentityString(string identityString)
    {
        return _authTicket;
    }
}