using System.Collections;
using NUnit.Framework;
using PurrNet;
using PurrNet.Transports;
using Resonance.Assemblies.LobbySystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerIdToLobbyMemberIdMapTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void PlayerIdToLobbyMemberIdMapTestsSimplePasses()
    {
        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator MapWithoutLobbyDataHolder_DoesNotSetIdMapping()
    {
        var networkManager = NetworkTestHelpers.SetupNetworkManager();
        yield return new WaitForSeconds(1);

        var map = NetworkTestHelpers.SetupNetworkIdentityOnNewGameObject<PlayerIdToLobbyMemberIdMap>(networkManager);

        var localPlayerId = networkManager.localPlayer;
        var lobbyMemberId = map.GetLobbyMemberId(localPlayerId);
        Assert.IsNull(lobbyMemberId);
    }
}
