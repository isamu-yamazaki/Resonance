using System.Collections.Generic;
using System.Threading.Tasks;
using Assemblies.ClientOrchestratorBridge;

public class FakeUserResolver : IPlatformUserResolver
{
    private readonly string _platformId;
    private readonly string _authTicket;
    private readonly List<string> _requestedAuthTicketIdentityStrings = new List<string>();

    public FakeUserResolver(
        string platformId,
        string authTicket
    )
    {
        _platformId = platformId;
        _authTicket = authTicket;
    }

    public IReadOnlyList<string> RequestedAuthTicketIdentityStrings => _requestedAuthTicketIdentityStrings;

    public string LastRequestedAuthTicketIdentityString =>
        _requestedAuthTicketIdentityStrings.Count == 0
            ? null
            : _requestedAuthTicketIdentityStrings[_requestedAuthTicketIdentityStrings.Count - 1];

    public string GetPlatformId()
    {
        return _platformId;
    }

    public Task<string> GetAuthTicketForIdentityString(string identityString)
    {
        _requestedAuthTicketIdentityStrings.Add(identityString);
        return Task.FromResult(_authTicket);
    }
}
