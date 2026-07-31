using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Resonance.Assemblies.ClientOrchestratorBridge;
using Resonance.Assemblies.LobbySystem;
using Resonance.Assemblies.OrchestratorHelpers;
using Resonance.Contracts;

public class ClientOrchestratorBridgeTests
{
    private ClientOrchestratorBridge _bridge;
    private FakeUserResolver _userResolver;
    private HttpClient _httpClient;
    private FakeHttpMessageHandler _httpHandler;
    private string _capturedRequestBody;

    private const string PlatformId = "platformId";
    private const string AuthTicket = "authTicket";
    private const string ServerAuthToken = "ServerAuthToken";
    private const string DedicatedServerHost = "http://127.0.0.1";
    private const int DedicatedServerPort = 7777;
    private const string OrchestratorBaseUrl = "http://127.0.0.1:9000";

    /// <remarks>
    /// The identity the orchestrator validates the ticket against; a mismatch here fails
    /// authentication on the server with no useful client-side symptom, so it is pinned.
    /// </remarks>
    private const string OrchestratorAuthIdentityString = "dev.bchen.ResonanceServerOrchestrator";

    /// <remarks>
    /// Spelled as a cast rather than <c>HttpStatusCode.TooManyRequests</c> so the test does not
    /// depend on which framework profile Unity compiles the test assembly against.
    /// </remarks>
    private const HttpStatusCode TooManyRequests = (HttpStatusCode)429;

    private const string AuthenticationRejectedMessage = "The authentication ticket was rejected.";
    private const string ValidationProblemTitle = "One or more validation errors occurred.";
    private const string ValidationProblemDetail = "PlatformLobbyId must not be empty.";

    /// <remarks>
    /// Shaped like a real Steam session ticket so a leak into an exception message is unmistakable.
    /// </remarks>
    private const string CredentialLikeAuthTicketHex = "14000000DEADBEEFCAFEF00D0BADC0DE";

    /// <remarks>
    /// A well-formed JSON integer that no <c>int</c> can hold. Reading one by converting it throws
    /// an <c>OverflowException</c>, which is not a JSON failure and would escape the bridge instead
    /// of degrading to the request failure an unusable body is reported as.
    /// </remarks>
    private const long IntegerBeyondInt32Range = 3000000000L;

    private static readonly Guid ExpectedMatchId = new Guid("8f1d0a3c-4b2e-4f5a-9c6d-7e8f9a0b1c2d");

    /// <remarks>
    /// Only ever elapses when the behaviour under test is broken: every assertion that uses it
    /// completes as soon as the awaited task settles.
    /// </remarks>
    private static readonly TimeSpan PendingOperationGuardTimeout = TimeSpan.FromSeconds(5);

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
        _capturedRequestBody = null;
    }

    #endregion

    #region Construction

    [Test]
    public void Constructor_ThrowsWhenTheHttpClientIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ClientOrchestratorBridge(null, _userResolver, Platform.Dummy)
        );
    }

    [Test]
    public void Constructor_ThrowsWhenTheHttpClientCarriesNoBaseAddress()
    {
        var clientWithoutBaseAddress = new HttpClient(
            new FakeHttpMessageHandler(_ => ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.OK)
            ));

        Assert.Throws<ArgumentException>(() =>
            new ClientOrchestratorBridge(clientWithoutBaseAddress, _userResolver, Platform.Dummy)
        );
    }

    [Test]
    public void Constructor_ThrowsWhenThePlatformUserResolverIsNull()
    {
        var client = new HttpClient(
            new FakeHttpMessageHandler(_ => ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.OK)
            ))
        {
            BaseAddress = new Uri(OrchestratorBaseUrl)
        };

        Assert.Throws<ArgumentNullException>(() => new ClientOrchestratorBridge(client, null, Platform.Dummy)
        );
    }

    [Test]
    public void BuildWithPlatform_ThrowsForAPlatformThatHasNoUserResolver()
    {
        var client = new HttpClient(
            new FakeHttpMessageHandler(_ => ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.OK)
            ))
        {
            BaseAddress = new Uri(OrchestratorBaseUrl)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ClientOrchestratorBridge.BuildWithPlatform((Platform)99, client)
        );
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

    [Test]
    public async Task GetJoinMatchDtoForLobby_RequestsTheAuthTicketForTheOrchestratorIdentity()
    {
        SetUpBridgeWithEmptyResponseAndDefaultUserResolver();

        await _bridge.GetJoinMatchDtoForLobby(GenerateLobby());

        Assert.AreEqual(
            OrchestratorAuthIdentityString,
            _userResolver.LastRequestedAuthTicketIdentityString
        );
    }

    [Test]
    public async Task GetJoinMatchDtoForLobby_IssuesNoHttpRequestOfItsOwn()
    {
        SetUpBridgeWithEmptyResponseAndDefaultUserResolver();

        await _bridge.GetJoinMatchDtoForLobby(GenerateLobby());

        AssertNoRequestWasIssued();
    }

    [Test]
    public async Task GetJoinMatchDtoForLobby_ReturnsAnEmptyRosterForALobbyWithNoMembers()
    {
        SetUpBridgeWithEmptyResponseAndDefaultUserResolver();

        var dto = await _bridge.GetJoinMatchDtoForLobby(GenerateLobby(memberCount: 0));

        Assert.IsNotNull(dto.ExpectedLobbyPlayers);
        Assert.IsEmpty(dto.ExpectedLobbyPlayers);
    }

    /// <remarks>
    /// A null member list means the caller does not yet know the roster. Sending an empty roster
    /// instead would have the orchestrator wait for a match that can never assemble.
    /// </remarks>
    [Test]
    public void GetJoinMatchDtoForLobby_RejectsALobbyWhoseMemberListIsNull()
    {
        SetUpBridgeWithEmptyResponseAndDefaultUserResolver();

        var lobbyWithoutMemberList = GenerateLobby();
        lobbyWithoutMemberList.Members = null;

        Assert.ThrowsAsync<ArgumentException>(() => _bridge.GetJoinMatchDtoForLobby(lobbyWithoutMemberList)
        );
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

    [Test]
    public async Task GetLeaveMatchDtoForLobby_RequestsTheAuthTicketForTheOrchestratorIdentity()
    {
        SetUpBridgeWithEmptyResponseAndDefaultUserResolver();

        await _bridge.GetLeaveMatchDtoForLobby(GenerateLobby());

        Assert.AreEqual(
            OrchestratorAuthIdentityString,
            _userResolver.LastRequestedAuthTicketIdentityString
        );
    }

    [Test]
    public async Task GetLeaveMatchDtoForLobby_IssuesNoHttpRequestOfItsOwn()
    {
        SetUpBridgeWithEmptyResponseAndDefaultUserResolver();

        await _bridge.GetLeaveMatchDtoForLobby(GenerateLobby());

        AssertNoRequestWasIssued();
    }

    #endregion

    #region JoinMatch success

    [Test]
    public async Task JoinMatch_CallsEndpointToReturnJoinMatchResultDtoInfo()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithBody(
            HttpStatusCode.OK,
            GenerateSerializedSuccessfulJoinMatchResultDto()
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();
        var result = await _bridge.JoinMatch(dto);

        Assert.IsNotNull(result);
        Assert.AreEqual(result.DedicatedServerHost, DedicatedServerHost);
        Assert.AreEqual(result.DedicatedServerPort, DedicatedServerPort);
        Assert.AreEqual(result.ServerAuthToken, ServerAuthToken);

        AssertExactlyOneRequestWasIssued();
        Assert.AreEqual(HttpMethod.Post, _httpHandler.LastRequest?.Method);
        Assert.AreEqual(
            new Uri($"{OrchestratorBaseUrl}/v1/matches/join"),
            _httpHandler.LastRequest?.RequestUri
        );
        Assert.AreEqual(JsonConvert.SerializeObject(dto), _capturedRequestBody);
    }

    /// <remarks>
    /// The orchestrator serializes with camelCase property names, which is not the shape the
    /// client's own serializer produces, so the body is written out literally here.
    /// </remarks>
    [Test]
    public async Task JoinMatch_ParsesASuccessBodyInTheServerCamelCaseWireFormat()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithBody(
            HttpStatusCode.OK,
            ClientOrchestratorResponseBuilder.SerializeJoinMatchResultInServerWireFormat(
                ExpectedMatchId,
                DedicatedServerHost,
                DedicatedServerPort,
                ServerAuthToken
            )
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();
        var result = await _bridge.JoinMatch(dto);

        Assert.AreEqual(ExpectedMatchId, result.MatchId);
        Assert.AreEqual(DedicatedServerHost, result.DedicatedServerHost);
        Assert.AreEqual(DedicatedServerPort, result.DedicatedServerPort);
        Assert.AreEqual(ServerAuthToken, result.ServerAuthToken);
    }

    /// <remarks>
    /// The request wire format is what the deployed orchestrator already accepts: PascalCase names
    /// and numeric enum values. Changing response parsing must not drag the request along with it.
    /// </remarks>
    [Test]
    public async Task JoinMatch_SendsThePascalCaseRequestBodyWithNumericPlatformValues()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithBody(
            HttpStatusCode.OK,
            GenerateSerializedSuccessfulJoinMatchResultDto()
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();
        await _bridge.JoinMatch(dto);

        var requestBody = JObject.Parse(_capturedRequestBody);
        var platformUserInformation = requestBody["PlatformUserInformation"];

        Assert.IsNotNull(platformUserInformation, "expected a PascalCase PlatformUserInformation");
        Assert.IsNotNull(platformUserInformation["PlatformUserId"]);
        Assert.IsNotNull(platformUserInformation["PlatformLobbyId"]);
        Assert.IsNotNull(platformUserInformation["AuthenticationTicketHex"]);
        Assert.AreEqual(JTokenType.Integer, platformUserInformation["Platform"].Type);
        Assert.AreEqual((int)Platform.Dummy, platformUserInformation["Platform"].Value<int>());

        var expectedLobbyPlayers = requestBody["ExpectedLobbyPlayers"];
        Assert.IsNotNull(expectedLobbyPlayers, "expected a PascalCase ExpectedLobbyPlayers");
        Assert.AreEqual(JTokenType.Integer, expectedLobbyPlayers[0]["Platform"].Type);
        Assert.IsNotNull(expectedLobbyPlayers[0]["Username"]);
        Assert.IsNotNull(expectedLobbyPlayers[0]["PlatformUserId"]);
    }

    /// <remarks>
    /// The orchestrator can be hosted under a path prefix. Resolving the endpoint against a base
    /// address that has no trailing slash must not drop the prefix's last segment.
    /// </remarks>
    [Test]
    public async Task JoinMatch_KeepsThePathPrefixOfABaseAddressWithoutATrailingSlash()
    {
        SetUpBridgeRespondingWith(
            ClientOrchestratorResponseBuilder.WithBody(
                HttpStatusCode.OK,
                GenerateSerializedSuccessfulJoinMatchResultDto()
            ),
            baseUrl: $"{OrchestratorBaseUrl}/orchestrator"
        );

        var dto = await BuildJoinMatchDtoForGeneratedLobby();
        await _bridge.JoinMatch(dto);

        Assert.AreEqual(
            new Uri($"{OrchestratorBaseUrl}/orchestrator/v1/matches/join"),
            _httpHandler.LastRequest?.RequestUri
        );
    }

    #endregion

    #region JoinMatch refused by the orchestrator

    [Test]
    public async Task JoinMatch_ThrowsJoinMatchFailedExceptionOn409Conflict()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.JoinFailure(
            HttpStatusCode.Conflict,
            nameof(JoinFailureReason.RosterMismatch),
            joinedCount: 1,
            expectedCount: 2
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<JoinMatchFailedException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(JoinFailureReason.RosterMismatch, thrownException.Reason);
        Assert.AreEqual(1, thrownException.JoinedCount);
        Assert.AreEqual(2, thrownException.ExpectedCount);
        Assert.AreEqual(HttpStatusCode.Conflict, thrownException.StatusCode);
        Assert.IsNull(thrownException.RetryAfter);
        AssertExactlyOneRequestWasIssued();
    }

    [TestCase(JoinFailureReason.RosterAssemblyTimedOut)]
    [TestCase(JoinFailureReason.ServerReadyTimedOut)]
    [TestCase(JoinFailureReason.RosterMismatch)]
    [TestCase(JoinFailureReason.PeerLeft)]
    [TestCase(JoinFailureReason.PeerAuthenticationFailed)]
    [TestCase(JoinFailureReason.PlayerInMultipleLobbies)]
    [TestCase(JoinFailureReason.ServerLaunchFailed)]
    [TestCase(JoinFailureReason.SupersededByReconnect)]
    [TestCase(JoinFailureReason.MatchAlreadyStarted)]
    [TestCase(JoinFailureReason.CapacityReached)]
    [TestCase(JoinFailureReason.OtherDataMismatch)]
    public async Task JoinMatch_ParsesEveryJoinFailureReasonFromItsServerStringForm(
        JoinFailureReason expectedReason
    )
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.JoinFailure(
            HttpStatusCode.Conflict,
            expectedReason.ToString(),
            joinedCount: 3,
            expectedCount: 4
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<JoinMatchFailedException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(expectedReason, thrownException.Reason);
        Assert.AreEqual(3, thrownException.JoinedCount);
        Assert.AreEqual(4, thrownException.ExpectedCount);
    }

    /// <remarks>
    /// Guards the case list above: a reason added to the contract without a case here would
    /// otherwise go untested and unnoticed.
    /// </remarks>
    [Test]
    public void JoinFailureReason_HasExactlyTheTenValuesCoveredByTheReasonParsingTest()
    {
        Assert.AreEqual(11, Enum.GetValues(typeof(JoinFailureReason)).Length);
    }

    [Test]
    public async Task JoinMatch_ThrowsJoinMatchFailedExceptionWithRetryAfterOnServiceUnavailable()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.JoinFailure(
            HttpStatusCode.ServiceUnavailable,
            nameof(JoinFailureReason.CapacityReached),
            joinedCount: 0,
            expectedCount: 2,
            retryAfter: TimeSpan.FromSeconds(5)
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<JoinMatchFailedException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(JoinFailureReason.CapacityReached, thrownException.Reason);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, thrownException.StatusCode);
        Assert.AreEqual(TimeSpan.FromSeconds(5), thrownException.RetryAfter);
        AssertExactlyOneRequestWasIssued();
    }

    [Test]
    public async Task JoinMatch_LeavesRetryAfterUnsetWhenServiceUnavailableCarriesNoRetryAfterHeader()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.JoinFailure(
            HttpStatusCode.ServiceUnavailable,
            nameof(JoinFailureReason.CapacityReached),
            joinedCount: 0,
            expectedCount: 2
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<JoinMatchFailedException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, thrownException.StatusCode);
        Assert.IsNull(thrownException.RetryAfter);
        AssertExactlyOneRequestWasIssued();
    }

    /// <remarks>
    /// A reason the client's contract does not know about is not a join failure the client can
    /// reason about, so it degrades to the generic request failure rather than guessing.
    /// </remarks>
    [Test]
    public async Task JoinMatch_FallsBackToOrchestratorRequestExceptionForAnUnrecognisedFailureReason()
    {
        var body = ClientOrchestratorResponseBuilder.SerializeJoinFailureInServerWireFormat(
            "SomethingBrandNew",
            joinedCount: 1,
            expectedCount: 2
        );
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithBody(HttpStatusCode.Conflict, body));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.Conflict, thrownException.StatusCode);
        Assert.AreEqual(body, thrownException.ResponseBody);
        AssertExactlyOneRequestWasIssued();
    }

    [TestCase("null", TestName = "JoinMatch_ThrowsOrchestratorRequestException_WhenConflictBodyIsTheLiteralNull")]
    [TestCase("[1,2,3]", TestName = "JoinMatch_ThrowsOrchestratorRequestException_WhenConflictBodyIsAnArray")]
    [TestCase("\"not a join failure\"",
        TestName = "JoinMatch_ThrowsOrchestratorRequestException_WhenConflictBodyIsABareString")]
    public async Task JoinMatch_ThrowsOrchestratorRequestExceptionWhenConflictBodyIsNotAJoinFailure(
        string responseBody
    )
    {
        SetUpBridgeRespondingWith(
            ClientOrchestratorResponseBuilder.WithBody(HttpStatusCode.Conflict, responseBody)
        );

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.Conflict, thrownException.StatusCode);
        Assert.AreEqual(responseBody, thrownException.ResponseBody);
        AssertExactlyOneRequestWasIssued();
    }

    /// <remarks>
    /// A body with no reason field would otherwise bind to the default enum value and be reported
    /// as a RosterAssemblyTimedOut that the orchestrator never said.
    /// </remarks>
    [Test]
    public async Task JoinMatch_ThrowsOrchestratorRequestExceptionWhenConflictBodyOmitsTheFailureReason()
    {
        const string bodyWithoutAReason = "{\"totally\":\"unrelated\"}";
        SetUpBridgeRespondingWith(
            ClientOrchestratorResponseBuilder.WithBody(HttpStatusCode.Conflict, bodyWithoutAReason)
        );

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.Conflict, thrownException.StatusCode);
        AssertExactlyOneRequestWasIssued();
    }

    /// <remarks>
    /// A roster count the client cannot hold describes a refusal it cannot represent, so it degrades
    /// to the generic request failure. It must not surface as the conversion's own
    /// <c>OverflowException</c>, and must not be reported as a refusal with counts the orchestrator
    /// never sent.
    /// </remarks>
    [Test]
    public async Task JoinMatch_ThrowsOrchestratorRequestExceptionWhenAConflictCountIsBeyondInt32Range()
    {
        var body = $"{{\"reason\":\"{nameof(JoinFailureReason.RosterMismatch)}\"," +
                   $"\"joinedCount\":{IntegerBeyondInt32Range},\"expectedCount\":2}}";
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithBody(HttpStatusCode.Conflict, body));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.Conflict, thrownException.StatusCode);
        Assert.AreEqual(body, thrownException.ResponseBody);
        AssertExactlyOneRequestWasIssued();
    }

    [Test]
    public async Task JoinMatch_ThrowsOrchestratorRequestExceptionWhenTheFailureReasonNumberIsBeyondInt32Range()
    {
        var body = $"{{\"reason\":{IntegerBeyondInt32Range},\"joinedCount\":1,\"expectedCount\":2}}";
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithBody(HttpStatusCode.Conflict, body));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.Conflict, thrownException.StatusCode);
        Assert.AreEqual(body, thrownException.ResponseBody);
        AssertExactlyOneRequestWasIssued();
    }

    /// <remarks>
    /// Every other property of the failure body is read case-insensitively; the reason name is read
    /// the same way, so a casing change on the wire does not turn a described refusal into an
    /// unreadable body.
    /// </remarks>
    [Test]
    public async Task JoinMatch_ReadsAFailureReasonNameWhoseCasingDiffersFromTheContracts()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.JoinFailure(
            HttpStatusCode.Conflict,
            "rosterMismatch",
            joinedCount: 1,
            expectedCount: 2
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<JoinMatchFailedException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(JoinFailureReason.RosterMismatch, thrownException.Reason);
        Assert.AreEqual(1, thrownException.JoinedCount);
        Assert.AreEqual(2, thrownException.ExpectedCount);
        AssertExactlyOneRequestWasIssued();
    }

    /// <remarks>
    /// The orchestrator names its reasons; it never sends the numeric form as a string. Accepting one
    /// would report a refusal — here <see cref="JoinFailureReason.PlayerInMultipleLobbies"/> — that
    /// nothing on the wire actually described.
    /// </remarks>
    [Test]
    public async Task JoinMatch_ThrowsOrchestratorRequestExceptionWhenTheFailureReasonIsANumericString()
    {
        var body = ClientOrchestratorResponseBuilder.SerializeJoinFailureInServerWireFormat(
            "5",
            joinedCount: 1,
            expectedCount: 2
        );
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithBody(HttpStatusCode.Conflict, body));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.Conflict, thrownException.StatusCode);
        Assert.AreEqual(body, thrownException.ResponseBody);
        AssertExactlyOneRequestWasIssued();
    }

    /// <remarks>
    /// A refusal has exactly one reason. A comma-separated list is not a shape the orchestrator sends,
    /// and combining the listed values yields a defined reason it never described.
    /// </remarks>
    [Test]
    public async Task JoinMatch_ThrowsOrchestratorRequestExceptionWhenTheFailureReasonIsACommaSeparatedList()
    {
        var body = ClientOrchestratorResponseBuilder.SerializeJoinFailureInServerWireFormat(
            $"{nameof(JoinFailureReason.RosterMismatch)},{nameof(JoinFailureReason.PeerLeft)}",
            joinedCount: 1,
            expectedCount: 2
        );
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithBody(HttpStatusCode.Conflict, body));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.Conflict, thrownException.StatusCode);
        Assert.AreEqual(body, thrownException.ResponseBody);
        AssertExactlyOneRequestWasIssued();
    }

    #endregion

    #region JoinMatch request failures

    [Test]
    public async Task JoinMatch_ThrowsOnHttpErrorCode()
    {
        SetUpBridgeRespondingWith(
            ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.InternalServerError)
        );

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.InternalServerError, thrownException.StatusCode);
        AssertExactlyOneRequestWasIssued();
    }

    [Test]
    public async Task JoinMatch_PreservesTheProblemDetailsBodyOfABadRequest()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.ProblemDetails(
            HttpStatusCode.BadRequest,
            ValidationProblemTitle,
            ValidationProblemDetail
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.BadRequest, thrownException.StatusCode);
        Assert.That(thrownException.ResponseBody, Does.Contain(ValidationProblemDetail));
        Assert.That(thrownException.ResponseBody, Does.Contain(ValidationProblemTitle));
        AssertExactlyOneRequestWasIssued();
    }

    /// <remarks>
    /// The orchestrator answers 401 with a bare JSON string rather than an object, which is exactly
    /// the shape that trips a deserializer expecting a DTO.
    /// </remarks>
    [Test]
    public async Task JoinMatch_PreservesTheBareStringBodyOfAnUnauthorizedResponse()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.BareJsonStringBody(
            HttpStatusCode.Unauthorized,
            AuthenticationRejectedMessage
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.Unauthorized, thrownException.StatusCode);
        Assert.AreEqual($"\"{AuthenticationRejectedMessage}\"", thrownException.ResponseBody);
        AssertExactlyOneRequestWasIssued();
    }

    [Test]
    public async Task JoinMatch_ReportsAnEmptyBodyWhenTheOrchestratorRateLimitsWithoutOne()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithoutBody(TooManyRequests));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(TooManyRequests, thrownException.StatusCode);
        Assert.IsNotNull(thrownException.ResponseBody);
        Assert.IsEmpty(thrownException.ResponseBody);
        AssertExactlyOneRequestWasIssued();
    }

    #endregion

    #region JoinMatch unreadable success bodies

    [TestCase("{\"matchId\":\"8f1d0a3c-4b2e-4f5a-9c6d-7e8f9a0b1c2d\",\"dedicatedServer",
        TestName = "JoinMatch_ThrowsOrchestratorRequestException_WhenSuccessBodyIsTruncated")]
    [TestCase("null", TestName = "JoinMatch_ThrowsOrchestratorRequestException_WhenSuccessBodyIsTheLiteralNull")]
    [TestCase("<html><body>502 Bad Gateway</body></html>",
        TestName = "JoinMatch_ThrowsOrchestratorRequestException_WhenSuccessBodyIsHtml")]
    public async Task JoinMatch_ThrowsOrchestratorRequestExceptionWhenTheSuccessBodyIsNotAResult(
        string responseBody
    )
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithBody(HttpStatusCode.OK, responseBody));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.JoinMatch(dto));

        Assert.AreEqual(HttpStatusCode.OK, thrownException.StatusCode);
        Assert.AreEqual(responseBody, thrownException.ResponseBody);
        AssertExactlyOneRequestWasIssued();
    }

    #endregion

    #region JoinMatch argument guards

    [Test]
    public void JoinMatch_RejectsANullDtoWithoutIssuingARequest()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.OK));

        Assert.ThrowsAsync<ArgumentNullException>(() => _bridge.JoinMatch(null));
        AssertNoRequestWasIssued();
    }

    #endregion

    #region LeaveMatch

    [Test]
    public async Task LeaveMatch_CallsEndpointAndExitsIfSucceeds()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.NoContent));

        var dto = await BuildLeaveMatchDtoForGeneratedLobby();

        await _bridge.LeaveMatch(dto);

        AssertExactlyOneRequestWasIssued();
        Assert.AreEqual(HttpMethod.Post, _httpHandler.LastRequest?.Method);
        Assert.AreEqual(
            new Uri($"{OrchestratorBaseUrl}/v1/matches/leave"),
            _httpHandler.LastRequest?.RequestUri
        );
        Assert.AreEqual(JsonConvert.SerializeObject(dto), _capturedRequestBody);
    }

    /// <remarks>
    /// The orchestrator answers 404 whenever the caller is not in a match — including every repeat
    /// leave — which is the state the caller was asking for, not a failure.
    /// </remarks>
    [Test]
    public async Task LeaveMatch_TreatsNotFoundAsAlreadyLeft()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.NotFound));

        var dto = await BuildLeaveMatchDtoForGeneratedLobby();

        Assert.DoesNotThrowAsync(() => _bridge.LeaveMatch(dto));
        AssertExactlyOneRequestWasIssued();
    }

    [Test]
    public async Task LeaveMatch_ThrowsOnHttpErrorCode()
    {
        SetUpBridgeRespondingWith(
            ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.InternalServerError)
        );

        var dto = await BuildLeaveMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.LeaveMatch(dto));

        Assert.AreEqual(HttpStatusCode.InternalServerError, thrownException.StatusCode);
        AssertExactlyOneRequestWasIssued();
    }

    [TestCase(HttpStatusCode.BadRequest, TestName = "LeaveMatch_ThrowsOrchestratorRequestException_OnBadRequest")]
    [TestCase(HttpStatusCode.Unauthorized, TestName = "LeaveMatch_ThrowsOrchestratorRequestException_OnUnauthorized")]
    [TestCase(TooManyRequests, TestName = "LeaveMatch_ThrowsOrchestratorRequestException_OnTooManyRequests")]
    public async Task LeaveMatch_ThrowsOrchestratorRequestExceptionForRejectedStatuses(
        HttpStatusCode statusCode
    )
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithoutBody(statusCode));

        var dto = await BuildLeaveMatchDtoForGeneratedLobby();

        var thrownException = Assert.ThrowsAsync<OrchestratorRequestException>(() => _bridge.LeaveMatch(dto));

        Assert.AreEqual(statusCode, thrownException.StatusCode);
        AssertExactlyOneRequestWasIssued();
    }

    [Test]
    public void LeaveMatch_RejectsANullDtoWithoutIssuingARequest()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.NoContent));

        Assert.ThrowsAsync<ArgumentNullException>(() => _bridge.LeaveMatch(null));
        AssertNoRequestWasIssued();
    }

    #endregion

    #region Cancellation

    [Test]
    public async Task JoinMatch_ThrowsWithoutIssuingARequestWhenTheCallersTokenIsAlreadyCancelled()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithBody(
            HttpStatusCode.OK,
            GenerateSerializedSuccessfulJoinMatchResultDto()
        ));

        var dto = await BuildJoinMatchDtoForGeneratedLobby();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(() => _bridge.JoinMatch(dto, cancellationTokenSource.Token)
        );
        AssertNoRequestWasIssued();
    }

    [Test]
    public async Task LeaveMatch_ThrowsWithoutIssuingARequestWhenTheCallersTokenIsAlreadyCancelled()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.NoContent));

        var dto = await BuildLeaveMatchDtoForGeneratedLobby();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(() => _bridge.LeaveMatch(dto, cancellationTokenSource.Token)
        );
        AssertNoRequestWasIssued();
    }

    /// <remarks>
    /// Asserts on the cancellation state the handler observes rather than on token identity:
    /// <see cref="HttpClient"/> always hands the handler a token linked to its own, never the
    /// caller's instance.
    /// </remarks>
    [Test]
    public async Task JoinMatch_ForwardsTheCallersCancellationTokenIntoSendAsync()
    {
        var parkedResponse = new ParkedOrchestratorResponse();
        SetUpBridgeParkingOn(parkedResponse);

        var dto = await BuildJoinMatchDtoForGeneratedLobby();
        using var cancellationTokenSource = new CancellationTokenSource();
        var joinTask = _bridge.JoinMatch(dto, cancellationTokenSource.Token);

        try
        {
            await AwaitWithinGuardTimeout(
                parkedResponse.RequestReachedHandler,
                "the join request never reached the message handler"
            );

            Assert.IsFalse(
                _httpHandler.LastObservedCancellationToken.IsCancellationRequested,
                "the handler saw a cancelled token before the caller cancelled anything"
            );

            cancellationTokenSource.Cancel();

            Assert.IsTrue(
                _httpHandler.LastObservedCancellationToken.IsCancellationRequested,
                "cancelling the caller's token did not reach the token handed to SendAsync"
            );
        }
        finally
        {
            parkedResponse.ReleaseAsCancelled();
            await SettleWithoutRethrowing(joinTask);
        }
    }

    /// <remarks>
    /// A real join parks for up to ~75 seconds; the handler here parks indefinitely instead, so the
    /// only thing that can unblock the assertion is the cancellation actually taking effect.
    /// </remarks>
    [Test]
    public async Task JoinMatch_FaultsPromptlyWhenCancelledWhileTheOrchestratorIsStillParking()
    {
        var parkedResponse = new ParkedOrchestratorResponse();
        SetUpBridgeParkingOn(parkedResponse);

        var dto = await BuildJoinMatchDtoForGeneratedLobby();
        using var cancellationTokenSource = new CancellationTokenSource();
        var joinTask = _bridge.JoinMatch(dto, cancellationTokenSource.Token);

        try
        {
            await AwaitWithinGuardTimeout(
                parkedResponse.RequestReachedHandler,
                "the join request never reached the message handler"
            );

            cancellationTokenSource.Cancel();

            await AwaitWithinGuardTimeout(
                SettleWithoutRethrowing(joinTask),
                "JoinMatch kept waiting on the parked request after the caller cancelled"
            );

            Assert.ThrowsAsync<TaskCanceledException>(() => joinTask);
            AssertExactlyOneRequestWasIssued();
        }
        finally
        {
            parkedResponse.ReleaseAsCancelled();
            await SettleWithoutRethrowing(joinTask);
        }
    }

    #endregion

    #region Credential leak guards

    [Test]
    public async Task JoinMatch_KeepsTheAuthenticationTicketOutOfARefusedJoinException()
    {
        await AssertJoinMatchFailureKeepsTheAuthenticationTicketOutOfItsException(
            ClientOrchestratorResponseBuilder.JoinFailure(
                HttpStatusCode.Conflict,
                nameof(JoinFailureReason.PeerAuthenticationFailed),
                joinedCount: 1,
                expectedCount: 2
            )
        );
    }

    [Test]
    public async Task JoinMatch_KeepsTheAuthenticationTicketOutOfARejectedRequestException()
    {
        await AssertJoinMatchFailureKeepsTheAuthenticationTicketOutOfItsException(
            ClientOrchestratorResponseBuilder.BareJsonStringBody(
                HttpStatusCode.Unauthorized,
                AuthenticationRejectedMessage
            )
        );
    }

    [Test]
    public async Task JoinMatch_KeepsTheAuthenticationTicketOutOfAnUnreadableSuccessException()
    {
        await AssertJoinMatchFailureKeepsTheAuthenticationTicketOutOfItsException(
            ClientOrchestratorResponseBuilder.WithBody(HttpStatusCode.OK, "{\"matchId\":\"trunc")
        );
    }

    #endregion

    #region Helpers

    private async Task AssertJoinMatchFailureKeepsTheAuthenticationTicketOutOfItsException(
        HttpResponseMessage response
    )
    {
        _userResolver = new FakeUserResolver(PlatformId, CredentialLikeAuthTicketHex);
        SetUpBridgeRespondingWith(response);

        var dto = await BuildJoinMatchDtoForGeneratedLobby();
        var thrownException = await CaptureExceptionFrom(() => _bridge.JoinMatch(dto));

        Assert.IsNotNull(thrownException, "expected the orchestrator's refusal to surface as an exception");
        Assert.That(
            _capturedRequestBody,
            Does.Contain(CredentialLikeAuthTicketHex),
            "the ticket must be in the request for this guard to mean anything"
        );
        Assert.That(thrownException.Message, Does.Not.Contain(CredentialLikeAuthTicketHex));
        Assert.That(thrownException.ToString(), Does.Not.Contain(CredentialLikeAuthTicketHex));
    }

    private static async Task<Exception> CaptureExceptionFrom(Func<Task> operation)
    {
        try
        {
            await operation();
            return null;
        }
        catch (Exception thrownException)
        {
            return thrownException;
        }
    }

    private static async Task AwaitWithinGuardTimeout(Task task, string failureDescription)
    {
        var firstToComplete = await Task.WhenAny(task, Task.Delay(PendingOperationGuardTimeout));
        Assert.AreSame(task, firstToComplete, failureDescription);
    }

    /// <remarks>
    /// Keeps a task's outcome observed without asserting on it, so a test that already made its
    /// assertion does not leave a faulted task dangling.
    /// </remarks>
    private static async Task SettleWithoutRethrowing(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception)
        {
            // intentionally ignored
        }
    }

    private void AssertExactlyOneRequestWasIssued()
    {
        Assert.AreEqual(1, _httpHandler.CallCount, "the bridge must not retry a request on its own");
    }

    private void AssertNoRequestWasIssued()
    {
        Assert.AreEqual(0, _httpHandler.CallCount);
    }

    private Task<JoinMatchDto> BuildJoinMatchDtoForGeneratedLobby()
    {
        return _bridge.GetJoinMatchDtoForLobby(GenerateLobby());
    }

    private Task<LeaveMatchDto> BuildLeaveMatchDtoForGeneratedLobby()
    {
        return _bridge.GetLeaveMatchDtoForLobby(GenerateLobby());
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

    private void SetUpBridgeParkingOn(
        ParkedOrchestratorResponse parkedResponse,
        string baseUrl = OrchestratorBaseUrl
    )
    {
        SetUpBridgeWithHandler(new FakeHttpMessageHandler((request, cancellationToken) =>
        {
            _capturedRequestBody = ReadRequestBody(request);
            return parkedResponse.ParkUntilReleasedOrCancelled(cancellationToken);
        }), baseUrl);
    }

    private void SetUpBridgeWithHandler(FakeHttpMessageHandler handler, string baseUrl)
    {
        _httpHandler = handler;
        _httpClient = new HttpClient(_httpHandler) { BaseAddress = new Uri(baseUrl) };
        _bridge = new ClientOrchestratorBridge(_httpClient, _userResolver, Platform.Dummy);
    }

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

    private void SetUpBridgeWithEmptyResponseAndDefaultUserResolver()
    {
        SetUpBridgeRespondingWith(ClientOrchestratorResponseBuilder.WithoutBody(HttpStatusCode.OK));
    }

    private static string GenerateSerializedSuccessfulJoinMatchResultDto()
    {
        var result = new JoinMatchResultDto(
            ExpectedMatchId,
            DedicatedServerHost,
            DedicatedServerPort,
            ServerAuthToken
        );

        return JsonConvert.SerializeObject(result);
    }

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

    private static Lobby GenerateLobby(
        int memberCount = 2
    )
    {
        var lobbyMemberList = GetLobbyMemberList(memberCount);
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