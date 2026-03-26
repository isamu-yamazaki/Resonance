using NUnit.Framework;
using Resonance.Assemblies.LobbySystem;

public class LobbyJsonTests
{
    [Test]
    public void LobbyJsonSerializationAndDeserialization_ResultsInEquality()
    {
        var lobby = LobbyFactory.Create(
            "Test Lobby",
            lobbyId: "12345",
            maxPlayers: 5,
            members: new(),
            properties: new()
        );

        var json = lobby.ToJson();
        var newLobby = Lobby.FromJson(json);
        Assert.AreEqual(lobby, newLobby);
    }
}
