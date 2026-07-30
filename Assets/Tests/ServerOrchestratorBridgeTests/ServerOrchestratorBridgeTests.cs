using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Resonance.Assemblies.OrchestratorHelpers;
using Resonance.Assemblies.ServerOrchestratorBridge;
using Resonance.Contracts;

public class ServerOrchestratorBridgeTests
{
    private ServerOrchestratorBridge _bridge;
    private HttpClient _httpClient;
    private FakeHttpMessageHandler _httpHandler;
    private string _capturedRequestBody;

    private List<MatchMemberDto> _mockMembers = new List<MatchMemberDto>
    {
        new(
            Platform.Dummy,
            "1",
            "Test user 1",
            "Auth token 1"
        ),
        new(
            Platform.Dummy,
            "2",
            "Test user 2",
            "Auth token 2"
        )
    };

    private const string OrchestratorBaseUrl = "http://127.0.0.1:9000";
    private const string MatchKey = "1";
    private const string MatchId = "1";

    #region SignalAsReady success

    [Test]
    public async Task SignalAsReady_CallsEndpointAndExits()
    {
        var response = new HttpResponseMessage();
        response.StatusCode = HttpStatusCode.NoContent;
        SetUpBridgeRespondingWith(response);

        await _bridge.SignalAsReady();

        Assert.AreEqual(1, _httpHandler.CallCount);
        Assert.AreEqual(HttpMethod.Post, _httpHandler.LastRequest?.Method);
        Assert.AreEqual(new Uri($"{OrchestratorBaseUrl}/v1/server/${MatchId}/ready"),
            _httpHandler.LastRequest?.RequestUri);
    }

    #endregion

    #region SignalAsReady failures

    // Since we're not dealing with a DTO on the error cases,
    // we can return just one exception/check all the cases here

    [TestCase(HttpStatusCode.Conflict)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Gone)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public void SignalAsReady_ThrowsOnHttpErrorCodes(HttpStatusCode statusCode)
    {
        var response = new HttpResponseMessage();
        response.StatusCode = statusCode;
        SetUpBridgeRespondingWith(response);

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.SignalAsReady());

        Assert.AreEqual(statusCode, thrownException.StatusCode);
        Assert.AreEqual(1, _httpHandler.CallCount);
    }

    #endregion

    #region GetMembers success

    [Test]
    public async Task GetMembers_ReturnsMembers()
    {
        SetUpBridgeRespondingWith(ServerOrchestratorResponseBuilder.WithBody(
            HttpStatusCode.OK,
            GenerateSerializedMemberList()
        ));

        var members = await _bridge.GetMembers();

        for (int i = 0; i < _mockMembers.Count; i++)
        {
            Assert.AreEqual(_mockMembers[i].Platform, members[i].Platform);
            Assert.AreEqual(_mockMembers[i].PlatformUserId, members[i].PlatformUserId);
            Assert.AreEqual(_mockMembers[i].ServerAuthToken, members[i].ServerAuthToken);
            Assert.AreEqual(_mockMembers[i].Username, members[i].Username);
        }

        Assert.AreEqual(JsonConvert.SerializeObject(members), _capturedRequestBody);
    }

    #endregion

    #region GetMembers failures

    [TestCase(HttpStatusCode.Conflict)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Gone)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public void GetMembers_ThrowsOnHttpErrorCodes(HttpStatusCode statusCode)
    {
        var response = new HttpResponseMessage();
        response.StatusCode = statusCode;
        SetUpBridgeRespondingWith(response);

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.GetMembers());
        Assert.AreEqual(statusCode, thrownException.StatusCode);
    }

    #endregion

    #region Helpers

    /// <remarks>
    /// Read inside the handler rather than from <c>LastRequest</c> afterwards, because
    /// <see cref="HttpClient"/> may dispose the request content once the exchange finishes.
    /// </remarks>
    private static string ReadRequestBody(HttpRequestMessage request)
    {
        return request.Content == null
            ? null
            : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    private void SetUpBridgeRespondingWith(
        HttpResponseMessage response,
        string baseUrl = OrchestratorBaseUrl
    )
    {
        SetUpBridgeWithHandler(new FakeHttpMessageHandler(request =>
        {
            _capturedRequestBody = ReadRequestBody(request);
            return response;
        }), baseUrl);
    }

    private void SetUpBridgeWithHandler(
        FakeHttpMessageHandler handler,
        string baseUrl = OrchestratorBaseUrl
    )
    {
        _httpHandler = handler;
        _httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        _bridge = new ServerOrchestratorBridge(_httpClient, MatchId, MatchKey);
    }

    private string GenerateSerializedMemberList()
    {
        return JsonConvert.SerializeObject(_mockMembers);
    }

    #endregion
}