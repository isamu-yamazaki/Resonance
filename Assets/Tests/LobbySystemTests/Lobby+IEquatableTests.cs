using System.Collections.Generic;
using NUnit.Framework;
using Resonance.Assemblies.LobbySystem;

public class LobbyIEquatableTests
{
    [Test]
    public void EqualLobbiesWithEmptyMembersAndProperties_AreEqual()
    {
        var a = LobbyTestsHelpers.CreateLobby();
        var b = LobbyTestsHelpers.CreateLobby();
        Assert.AreEqual(a, b);
    }

    [Test]
    public void LobbiesWithSameMembers_AreEqual()
    {
        var a = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser> { LobbyTestsHelpers.CreateUser("u1"), LobbyTestsHelpers.CreateUser("u2") });
        var b = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser> { LobbyTestsHelpers.CreateUser("u1"), LobbyTestsHelpers.CreateUser("u2") });
        Assert.AreEqual(a, b);
    }

    [Test]
    public void LobbiesWithDifferentMemberCount_AreNotEqual()
    {
        var a = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser> { LobbyTestsHelpers.CreateUser("u1") });
        var b = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser> { LobbyTestsHelpers.CreateUser("u1"), LobbyTestsHelpers.CreateUser("u2") });
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void LobbiesWithSameMemberCountButDifferentMemberData_AreNotEqual()
    {
        var a = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser> { LobbyTestsHelpers.CreateUser("u1", isReady: false) });
        var b = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser> { LobbyTestsHelpers.CreateUser("u1", isReady: true) });
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void LobbiesWithSameProperties_AreEqual()
    {
        var a = LobbyTestsHelpers.CreateLobby(properties: new Dictionary<string, string> { { "key", "value" } });
        var b = LobbyTestsHelpers.CreateLobby(properties: new Dictionary<string, string> { { "key", "value" } });
        Assert.AreEqual(a, b);
    }

    [Test]
    public void LobbiesWithDifferentPropertyValues_AreNotEqual()
    {
        var a = LobbyTestsHelpers.CreateLobby(properties: new Dictionary<string, string> { { "key", "valueA" } });
        var b = LobbyTestsHelpers.CreateLobby(properties: new Dictionary<string, string> { { "key", "valueB" } });
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void LobbiesWithDifferentPropertyKeys_AreNotEqual()
    {
        var a = LobbyTestsHelpers.CreateLobby(properties: new Dictionary<string, string> { { "keyA", "value" } });
        var b = LobbyTestsHelpers.CreateLobby(properties: new Dictionary<string, string> { { "keyB", "value" } });
        Assert.AreNotEqual(a, b);
    }
}
