using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Resonance.Assemblies.ClientOrchestratorBridge;
using Resonance.Assemblies.LobbySystem;
using Resonance.Contracts;
using UnityEngine;
using UnityEngine.TestTools;

public class ClientOrchestratorBridgeTests
{
    private ClientOrchestratorBridge _bridge;
    private FakeUserResolver _userResolver;
    private HttpClient _httpClient;
    private FakeHttpMessageHandler _httpHandler;

    private const string PlatformId = "platformId";
    private const string AuthTicket = "authTicket";
    private const string ServerAuthToken = "ServerAuthToken";
    private const string DedicatedServerHost = "http://127.0.0.1";
    private const int DedicatedServerPort = 7777;

    #region Lifecycle

    [SetUp]
    public void Setup()
    {
        _userResolver = new FakeUserResolver(PlatformId, AuthTicket);
        // set up orchestrator bridge in-test for mocking different requests
    }

    [TearDown]
    public void TearDown()
    {
        _bridge = null;
        _userResolver = null;
        _httpClient = null;
        _httpHandler = null;
    }

    #endregion

    #region GetJoinMatchDtoForLobby

    [Test]
    public async Task GetJoinMatchDtoForLobby_ReturnsExpectedLobbyInformation()
    {
        SetUpBridgeWithEmptyResponseAndDefaultUserResolver();

        var lobby = GenerateLobby();
        var lobbyMemberList = lobby.Members;
        var dto = await _bridge.GetJoinMatchDtoForLobby(lobby);

        Assert.IsNotNull(dto);

        for (int i = 0; i < lobbyMemberList.Count; i++)
        {
            Assert.AreEqual(lobbyMemberList[i].Id, dto.ExpectedLobbyPlayers[i].PlatformUserId);
            Assert.AreEqual(lobbyMemberList[i].DisplayName, dto.ExpectedLobbyPlayers[i].Username);
            Assert.AreEqual(Platform.Dummy, dto.ExpectedLobbyPlayers[i].Platform);
        }
    }

    [Test]
    public async Task GetJoinMatchDtoForLobby_SetsCorrectPlatformUserInformation()
    {
        SetUpBridgeWithEmptyResponseAndDefaultUserResolver();
        var lobby = GenerateLobby();

        var dto = await _bridge.GetJoinMatchDtoForLobby(lobby);
        Assert.AreEqual(PlatformId, dto.PlatformUserInformation.PlatformUserId);
        Assert.AreEqual(AuthTicket, dto.PlatformUserInformation.AuthenticationTicketHex);
        Assert.AreEqual(Platform.Dummy, dto.PlatformUserInformation.Platform);
        Assert.AreEqual(lobby.LobbyId, dto.PlatformUserInformation.PlatformLobbyId);
    }

    #endregion

    #region GetLeaveMatchDtoForLobby

    [Test]
    public async Task GetLeaveMatchDtoForLobby_SetsCorrectPlatformUserInformation()
    {
        SetUpBridgeWithEmptyResponseAndDefaultUserResolver();

        var lobby = GenerateLobby();
        var dto = await _bridge.GetLeaveMatchDtoForLobby(lobby);
        Assert.AreEqual(PlatformId, dto.PlatformUserInformation.PlatformUserId);
        Assert.AreEqual(AuthTicket, dto.PlatformUserInformation.AuthenticationTicketHex);
        Assert.AreEqual(Platform.Dummy, dto.PlatformUserInformation.Platform);
        Assert.AreEqual(lobby.LobbyId, dto.PlatformUserInformation.PlatformLobbyId);
    }

    #endregion

    #region JoinMatch

    [Test]
    public async Task JoinMatch_CallsEndpointToReturnJoinMatchResultDtoInfo()
    {
        _httpHandler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(GenerateSerializedSuccessfulJoinMatchResultDto())
        });
        _httpClient = new HttpClient(_httpHandler);
        _bridge = new ClientOrchestratorBridge(_httpClient, _userResolver, Platform.Dummy);

        var lobby = GenerateLobby();

        var dto = await _bridge.GetJoinMatchDtoForLobby(lobby);
        var result = await _bridge.JoinMatch(dto);

        Assert.IsNotNull(result);
        Assert.AreEqual(result.DedicatedServerHost, DedicatedServerHost);
        Assert.AreEqual(result.DedicatedServerPort, DedicatedServerPort);
        Assert.AreEqual(result.ServerAuthToken, ServerAuthToken);

        var expectedRequestMessage = new HttpRequestMessage();
        expectedRequestMessage.Method = HttpMethod.Post;
        expectedRequestMessage.RequestUri = new Uri($"{DedicatedServerHost}:{DedicatedServerPort}/v1/matches/join");
        expectedRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(dto));

        Assert.AreEqual(1, _httpHandler.CallCount);
        Assert.AreEqual(expectedRequestMessage.Method, _httpHandler.LastRequest?.Method);
        Assert.AreEqual(expectedRequestMessage.RequestUri, _httpHandler.LastRequest?.RequestUri);
        Assert.AreEqual(expectedRequestMessage.Content.ReadAsStringAsync().Result, _httpHandler.LastRequest?.Content.ReadAsStringAsync().Result);
    }


    [Test]
    public async Task JoinMatch_ThrowsOnHttpErrorCode()
    {
        _httpHandler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        _httpClient = new HttpClient(_httpHandler);
        _bridge = new ClientOrchestratorBridge(_httpClient, _userResolver, Platform.Dummy);

        var lobby = GenerateLobby();

        var dto = await _bridge.GetJoinMatchDtoForLobby(lobby);

        Assert.ThrowsAsync<HttpRequestException>(() => _bridge.JoinMatch(dto));
        Assert.AreEqual(1, _httpHandler.CallCount);
    }

    #endregion

    #region LeaveMatch

    [Test]
    public async Task LeaveMatch_CallsEndpointAndExitsIfSucceeds()
    {
        _httpHandler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        _httpClient = new HttpClient(_httpHandler);
        _bridge = new ClientOrchestratorBridge(_httpClient, _userResolver, Platform.Dummy);

        var lobby = GenerateLobby();

        var dto = await _bridge.GetLeaveMatchDtoForLobby(lobby);

        await _bridge.LeaveMatch(dto);

        var expectedRequestMessage = new HttpRequestMessage();
        expectedRequestMessage.Method = HttpMethod.Post;
        expectedRequestMessage.RequestUri = new Uri($"{DedicatedServerHost}:{DedicatedServerPort}/v1/matches/leave");
        expectedRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(dto));

        Assert.AreEqual(1, _httpHandler.CallCount);
        Assert.AreEqual(expectedRequestMessage.Method, _httpHandler.LastRequest?.Method);
        Assert.AreEqual(expectedRequestMessage.RequestUri, _httpHandler.LastRequest?.RequestUri);
        Assert.AreEqual(expectedRequestMessage.Content.ReadAsStringAsync().Result, _httpHandler.LastRequest?.Content.ReadAsStringAsync().Result);
    }

    [Test]
    public async Task LeaveMatch_ThrowsOnHttpErrorCode()
    {
        _httpHandler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        _httpClient = new HttpClient(_httpHandler);
        _bridge = new ClientOrchestratorBridge(_httpClient, _userResolver, Platform.Dummy);

        var lobby = GenerateLobby();

        var dto = await _bridge.GetLeaveMatchDtoForLobby(lobby);

        Assert.ThrowsAsync<HttpRequestException>(() => _bridge.LeaveMatch(dto));

        var expectedRequestMessage = new HttpRequestMessage();
        expectedRequestMessage.Method = HttpMethod.Post;
        expectedRequestMessage.RequestUri = new Uri($"{DedicatedServerHost}:{DedicatedServerPort}/v1/matches/leave");
        expectedRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(dto));

        Assert.AreEqual(1, _httpHandler.CallCount);
    }

    #endregion

    #region Helpers

    private static List<LobbyUser> GetLobbyMemberList(
        int count = 2
    )
    {
        List<LobbyUser> lobbyMemberList = new List<LobbyUser>();

        for (int i = 0; i < count; i++)
        {
            lobbyMemberList.Add(new LobbyUser
            {
                Id = (i + 1).ToString(),
                DisplayName = $"TestUser{i + 1}",
                IsReady = i % 2 == 0,
                IsOwner = i == 0
            });
        }

        return lobbyMemberList;
    }

    private void SetUpBridgeWithEmptyResponseAndDefaultUserResolver()
    {
        _httpHandler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        _httpClient = new HttpClient(_httpHandler);
        _bridge = new ClientOrchestratorBridge(_httpClient, _userResolver, Platform.Dummy);
    }

    private static string GenerateSerializedSuccessfulJoinMatchResultDto()
    {
        var result = new JoinMatchResultDto(
            Guid.NewGuid(),
            DedicatedServerHost,
            DedicatedServerPort,
            ServerAuthToken
        );

        return JsonConvert.SerializeObject(result);
    }

    private static Lobby GenerateLobby()
    {
        var lobbyMemberList = GetLobbyMemberList();
        var lobby = new Lobby
        {
            Name = "Test Lobby",
            LobbyId = "1234567890",
            IsValid = true,
            MaxPlayers = 10,
            Members = lobbyMemberList
        };
        return lobby;
    }

    #endregion
}