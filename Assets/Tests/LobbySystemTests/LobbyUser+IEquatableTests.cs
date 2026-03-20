using NUnit.Framework;
using Resonance.Assemblies.LobbySystem;

public class LobbyUserIEquatableTests
{
    [Test]
    public void UsersWithSameFields_AreEqual()
    {
        var a = LobbyTestsHelpers.CreateUser("u1", "Player", isReady: false, isOwner: false);
        var b = LobbyTestsHelpers.CreateUser("u1", "Player", isReady: false, isOwner: false);
        Assert.AreEqual(a, b);
    }

    [Test]
    public void UsersDifferingOnlyInIsOwner_AreNotEqual()
    {
        var a = LobbyTestsHelpers.CreateUser(isOwner: false);
        var b = LobbyTestsHelpers.CreateUser(isOwner: true);
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void UsersDifferingOnlyInIsReady_AreNotEqual()
    {
        var a = LobbyTestsHelpers.CreateUser(isReady: false);
        var b = LobbyTestsHelpers.CreateUser(isReady: true);
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void UsersDifferingOnlyInId_AreNotEqual()
    {
        var a = LobbyTestsHelpers.CreateUser(id: "u1");
        var b = LobbyTestsHelpers.CreateUser(id: "u2");
        Assert.AreNotEqual(a, b);
    }
}
