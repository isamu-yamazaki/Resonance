using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PurrNet;
using PurrNet.Modules;
using Resonance.Assemblies.LobbySystem;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerIdToLobbyMemberIdMapTests
{
    private NetworkManager networkManager;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        yield return NetworkTestHelpers.SetupNetworkManager(nm => networkManager = nm);
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        Object.Destroy(networkManager.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator MapWithoutLobbyDataHolder_DoesNotSetIdMapping()
    {
        PlayerIdToLobbyMemberIdMap map = null;
        yield return NetworkTestHelpers.SetupNetworkIdentityOnNewGameObject<PlayerIdToLobbyMemberIdMap>(networkManager, c => map = c);

        var localPlayerId = networkManager.localPlayer;
        var lobbyMemberId = map.GetLobbyMemberId(localPlayerId);
        Assert.IsNull(lobbyMemberId);

        Object.DestroyImmediate(map.gameObject);
    }

    [UnityTest]
    public IEnumerator MapWithLocalLobbyDataHolder_SetsIdMapping()
    {
        // if there is a local LobbyDataHolder, use that to set the ID mapping
        var lobbyDataHolderGameObject = new GameObject("LobbyDataHolder");
        var lobbyDataHolder = lobbyDataHolderGameObject.AddComponent<LobbyDataHolder>();

        string localLobbyMemberId = "1";
        lobbyDataHolder.SetLocalUserId(localLobbyMemberId);
        lobbyDataHolder.SetCurrentLobby(new() {});

        PlayerIdToLobbyMemberIdMap map = null;
        yield return NetworkTestHelpers.SetupNetworkIdentityOnNewGameObject<PlayerIdToLobbyMemberIdMap>(networkManager, c => map = c);

        var localPlayerId = networkManager.localPlayer;
        var lobbyMemberId = map.GetLobbyMemberId(localPlayerId);
        Assert.AreEqual(localLobbyMemberId, lobbyMemberId);

        Object.DestroyImmediate(map.gameObject);
        Object.DestroyImmediate(lobbyDataHolder.gameObject);
    }

    [UnityTest]
    public IEnumerator GetDisplayName_WithLocalLobbyDataHolderAndMember_ReturnsDisplayName()
    {
        var lobbyDataHolderGameObject = new GameObject("LobbyDataHolder");
        var lobbyDataHolder = lobbyDataHolderGameObject.AddComponent<LobbyDataHolder>();

        string localLobbyMemberId = "1";
        string localDisplayName = "TestUser";
        lobbyDataHolder.SetLocalUserId(localLobbyMemberId);
        lobbyDataHolder.SetCurrentLobby(new Lobby
        {
            Members = new List<LobbyUser>
            {
                new LobbyUser { Id = localLobbyMemberId, DisplayName = localDisplayName }
            }
        });

        PlayerIdToLobbyMemberIdMap map = null;
        yield return NetworkTestHelpers.SetupNetworkIdentityOnNewGameObject<PlayerIdToLobbyMemberIdMap>(networkManager, c => map = c);

        var localPlayerId = networkManager.localPlayer;
        var displayName = map.GetDisplayName(localPlayerId);
        Assert.AreEqual(localDisplayName, displayName);

        Object.DestroyImmediate(map.gameObject);
        Object.DestroyImmediate(lobbyDataHolder.gameObject);
    }

    [UnityTest]
    public IEnumerator PlayerLeave_RemovesIdMapping()
    {
        PlayerIdToLobbyMemberIdMap map = null;
        yield return NetworkTestHelpers.SetupNetworkIdentityOnNewGameObject<PlayerIdToLobbyMemberIdMap>(networkManager, c => map = c);

        var playersManager = networkManager.GetModule<PlayersManager>(asServer: true);
        PlayerID botPlayer = playersManager.CreateBot();
        yield return null;

        string expectedBotMemberId = "bot-member-id";
        map.RegisterLobbyMemberIdWithBotId(botPlayer, expectedBotMemberId);
        yield return null;

        Assert.AreEqual(expectedBotMemberId, map.GetLobbyMemberId(botPlayer));

        playersManager.KickPlayer(botPlayer);
        yield return null;

        Assert.IsNull(map.GetLobbyMemberId(botPlayer));

        Object.DestroyImmediate(map.gameObject);
    }
}
