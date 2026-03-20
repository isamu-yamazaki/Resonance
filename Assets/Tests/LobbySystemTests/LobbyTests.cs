using System.Collections.Generic;
using NUnit.Framework;
using Resonance.Assemblies.LobbySystem;

public class LobbyTests
{
    #region OwnerId

    [Test]
    public void OwnerId_ReturnsNull_WhenMembersListIsEmpty()
    {
        var lobby = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser>());
        Assert.IsNull(lobby.OwnerId);
    }

    [Test]
    public void OwnerId_ReturnsNull_WhenNoMemberIsOwner()
    {
        var lobby = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser>
        {
            LobbyTestsHelpers.CreateUser("u1", isOwner: false),
            LobbyTestsHelpers.CreateUser("u2", isOwner: false),
        });
        Assert.IsNull(lobby.OwnerId);
    }

    [Test]
    public void OwnerId_ReturnsOwnerId_WhenOneOwnerExists()
    {
        var lobby = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser>
        {
            LobbyTestsHelpers.CreateUser("u1", isOwner: false),
            LobbyTestsHelpers.CreateUser("u2", isOwner: true),
        });
        Assert.AreEqual("u2", lobby.OwnerId);
    }

    #endregion

    #region IsOwner

    [Test]
    public void IsOwner_ReturnsTrue_ForOwnerUserId()
    {
        var lobby = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser>
        {
            LobbyTestsHelpers.CreateUser("u1", isOwner: true),
        });
        Assert.IsTrue(lobby.IsOwner("u1"));
    }

    [Test]
    public void IsOwner_ReturnsFalse_ForNonOwnerUserId()
    {
        var lobby = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser>
        {
            LobbyTestsHelpers.CreateUser("u1", isOwner: false),
        });
        Assert.IsFalse(lobby.IsOwner("u1"));
    }

    [Test]
    public void IsOwner_ReturnsFalse_WhenUserNotInMembersList()
    {
        var lobby = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser>
        {
            LobbyTestsHelpers.CreateUser("u1", isOwner: true),
        });
        Assert.IsFalse(lobby.IsOwner("u2"));
    }

    [Test]
    public void IsOwner_ReturnsFalse_WhenMembersListIsEmpty()
    {
        var lobby = LobbyTestsHelpers.CreateLobby(members: new List<LobbyUser>());
        Assert.IsFalse(lobby.IsOwner("u1"));
    }

    #endregion
}
