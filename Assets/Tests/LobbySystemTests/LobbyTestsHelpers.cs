using System.Collections.Generic;
using Resonance.Assemblies.LobbySystem;

public static class LobbyTestsHelpers
{
    public static Lobby CreateLobby(List<LobbyUser> members = null, Dictionary<string, string> properties = null)
    {
        return LobbyFactory.Create(
            name: "Test Lobby",
            lobbyId: "123",
            lobbyCode: "ABC",
            maxPlayers: 4,
            members: members ?? new List<LobbyUser>(),
            properties: properties ?? new Dictionary<string, string>()
        );
    }

    public static LobbyUser CreateUser(string id = "u1", string displayName = "Player", bool isReady = false, bool isOwner = false)
    {
        return new LobbyUser { Id = id, DisplayName = displayName, IsReady = isReady, IsOwner = isOwner };
    }
}
