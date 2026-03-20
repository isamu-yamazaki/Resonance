using System.Collections.Generic;
using NUnit.Framework;
using Resonance.Assemblies.LobbySystem;

public class LobbyIEquatableTests
{
    private static Lobby CreateLobby(List<LobbyUser> members = null, Dictionary<string, string> properties = null)
    {
        return LobbyFactory.Create(
            name: "Test Lobby",
            lobbyId: "123",
            lobbyCode: "ABC",
            maxPlayers: 4,
            isOwner: true,
            members: members ?? new List<LobbyUser>(),
            properties: properties ?? new Dictionary<string, string>()
        );
    }

    private static LobbyUser CreateUser(string id = "u1", string displayName = "Player", bool isReady = false)
    {
        return new LobbyUser { Id = id, DisplayName = displayName, IsReady = isReady };
    }

    [Test]
    public void EqualLobbiesWithEmptyMembersAndProperties_AreEqual()
    {
        var a = CreateLobby();
        var b = CreateLobby();
        Assert.AreEqual(a, b);
    }

    [Test]
    public void LobbiesWithSameMembers_AreEqual()
    {
        var members = new List<LobbyUser> { CreateUser("u1"), CreateUser("u2") };
        var a = CreateLobby(members: new List<LobbyUser> { CreateUser("u1"), CreateUser("u2") });
        var b = CreateLobby(members: new List<LobbyUser> { CreateUser("u1"), CreateUser("u2") });
        Assert.AreEqual(a, b);
    }

    [Test]
    public void LobbiesWithDifferentMemberCount_AreNotEqual()
    {
        var a = CreateLobby(members: new List<LobbyUser> { CreateUser("u1") });
        var b = CreateLobby(members: new List<LobbyUser> { CreateUser("u1"), CreateUser("u2") });
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void LobbiesWithSameMemberCountButDifferentMemberData_AreNotEqual()
    {
        var a = CreateLobby(members: new List<LobbyUser> { CreateUser("u1", isReady: false) });
        var b = CreateLobby(members: new List<LobbyUser> { CreateUser("u1", isReady: true) });
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void LobbiesWithSameProperties_AreEqual()
    {
        var a = CreateLobby(properties: new Dictionary<string, string> { { "key", "value" } });
        var b = CreateLobby(properties: new Dictionary<string, string> { { "key", "value" } });
        Assert.AreEqual(a, b);
    }

    [Test]
    public void LobbiesWithDifferentPropertyValues_AreNotEqual()
    {
        var a = CreateLobby(properties: new Dictionary<string, string> { { "key", "valueA" } });
        var b = CreateLobby(properties: new Dictionary<string, string> { { "key", "valueB" } });
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void LobbiesWithDifferentPropertyKeys_AreNotEqual()
    {
        var a = CreateLobby(properties: new Dictionary<string, string> { { "keyA", "value" } });
        var b = CreateLobby(properties: new Dictionary<string, string> { { "keyB", "value" } });
        Assert.AreNotEqual(a, b);
    }
}
