using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assemblies.ClientOrchestratorBridge;
using NUnit.Framework;

public class SteamAuthTicketRequesterTests
{
    private FakeSteamAuthTicketApi _steamAuthTicketApi;

    private const string IdentityString = "resonance-orchestrator";
    private const uint IssuedAuthTicketHandle = 42u;
    private const uint ForeignAuthTicketHandle = 4242u;
    private const int NonOkSteamResultCode = 15;

    /// <summary>Long enough that a correctly implemented requester never reaches it in a test.</summary>
    private static readonly TimeSpan TicketResponseTimeoutThatShouldNeverElapse = TimeSpan.FromSeconds(5);

    /// <summary>Short enough that the timeout path is exercised without a meaningful wall-clock wait.</summary>
    private static readonly TimeSpan TicketResponseTimeoutThatElapsesImmediately = TimeSpan.FromMilliseconds(1);

    #region Lifecycle

    [SetUp]
    public void Setup()
    {
        _steamAuthTicketApi = new FakeSteamAuthTicketApi
        {
            AuthTicketHandleToIssue = IssuedAuthTicketHandle
        };
    }

    [TearDown]
    public void TearDown()
    {
        _steamAuthTicketApi = null;
    }

    #endregion

    #region Case 1 - a matching OK response completes with the ticket hex

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_CompletesWithHexOfTicketBytes_WhenResponseForIssuedHandleIsOk()
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(CreateOkResponseCarryingTicketBytes(IssuedAuthTicketHandle, 0xDE, 0xAD, 0xBE, 0xEF));

        Assert.AreEqual("DEADBEEF", await ticketTask);
        Assert.AreEqual(1, _steamAuthTicketApi.TotalSubscriptionCount);
        Assert.AreEqual(0, _steamAuthTicketApi.LiveSubscriptionCount);
        CollectionAssert.IsEmpty(
            _steamAuthTicketApi.CancelledAuthTicketHandles,
            "A successfully obtained ticket must survive so the orchestrator can validate it."
        );
    }

    #endregion

    #region Case 2 - the identity string reaches Steam verbatim

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_PassesIdentityStringToSteamVerbatim()
    {
        const string identityStringWithMixedCaseAndPunctuation = "Resonance:Match/Join+1234567890";
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(identityStringWithMixedCaseAndPunctuation);

        CollectionAssert.AreEqual(
            new[] { identityStringWithMixedCaseAndPunctuation },
            _steamAuthTicketApi.RequestedIdentityStrings
        );

        DeliverResponse(CreateOkResponseCarryingTicketBytes(IssuedAuthTicketHandle, 0x01));
        await ticketTask;
    }

    #endregion

    #region Case 3 - hex is uppercase, zero padded and separator free

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_RendersTicketBytesAsUppercaseHexWithoutSeparators()
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(CreateOkResponseCarryingTicketBytes(IssuedAuthTicketHandle, 0x00, 0x0F, 0xAB));

        Assert.AreEqual("000FAB", await ticketTask);
    }

    #endregion

    #region Case 4 - a single meaningful byte renders as two hex characters

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_RendersTwoHexCharacters_WhenTicketIsASingleByte()
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(CreateOkResponseCarryingTicketBytes(IssuedAuthTicketHandle, 0x07));

        var ticketHex = await ticketTask;

        Assert.AreEqual("07", ticketHex);
        Assert.AreEqual(2, ticketHex.Length);
    }

    #endregion

    #region Case 5 - a completely full ticket buffer is not truncated

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_RendersEveryByte_WhenTicketFillsTheEntireBuffer()
    {
        var completelyFullTicketBuffer = CreateTicketBufferFilledWithARepeatingPattern();
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(new SteamAuthTicketResponse(
            IssuedAuthTicketHandle,
            SteamAuthTicketResponse.ResultCodeOk,
            completelyFullTicketBuffer,
            SteamAuthTicketResponse.TicketBufferLength
        ));

        var ticketHex = await ticketTask;

        Assert.AreEqual(SteamAuthTicketResponse.TicketBufferLength * 2, ticketHex.Length);
        Assert.AreEqual(
            RenderExpectedUppercaseHex(completelyFullTicketBuffer, SteamAuthTicketResponse.TicketBufferLength),
            ticketHex
        );
    }

    #endregion

    #region Case 6 - an empty ticket is a typed failure rather than an empty string

    [Test]
    public void RequestAuthTicketHexForIdentityString_FailsWithTicketWasEmpty_WhenNoTicketBytesAreMeaningful()
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(CreateOkResponseCarryingTicketBytes(IssuedAuthTicketHandle));

        var failure = Assert.ThrowsAsync<SteamAuthTicketRequestFailedException>(() => ticketTask);

        Assert.AreEqual(SteamAuthTicketRequestFailureKind.TicketWasEmpty, failure.FailureKind);
        Assert.AreEqual(0, _steamAuthTicketApi.LiveSubscriptionCount);
        CollectionAssert.AreEqual(new[] { IssuedAuthTicketHandle }, _steamAuthTicketApi.CancelledAuthTicketHandles);
    }

    #endregion

    #region Case 7 - bytes beyond the meaningful length never reach the output

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_ExcludesBufferContentBeyondTheMeaningfulTicketLength()
    {
        const byte trailingGarbageByte = 0xFF;
        var ticketBufferWithTrailingGarbage = CreateTicketBufferStartingWith(
            new byte[] { 0xAA, 0xBB },
            trailingGarbageByte
        );
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(new SteamAuthTicketResponse(
            IssuedAuthTicketHandle,
            SteamAuthTicketResponse.ResultCodeOk,
            ticketBufferWithTrailingGarbage,
            2
        ));

        var ticketHex = await ticketTask;

        Assert.AreEqual("AABB", ticketHex);
        StringAssert.DoesNotContain("FF", ticketHex);
    }

    #endregion

    #region Case 8 - an out of range meaningful length is a typed failure

    [TestCase(-1)]
    [TestCase(SteamAuthTicketResponse.TicketBufferLength + 1)]
    public void RequestAuthTicketHexForIdentityString_FailsWithTicketLengthWasOutOfRange_WhenReportedLengthCannotFitTheBuffer(
        int meaningfulTicketLengthOutsideTheBuffer
    )
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(new SteamAuthTicketResponse(
            IssuedAuthTicketHandle,
            SteamAuthTicketResponse.ResultCodeOk,
            CreateTicketBufferFilledWithARepeatingPattern(),
            meaningfulTicketLengthOutsideTheBuffer
        ));

        var failure = Assert.ThrowsAsync<SteamAuthTicketRequestFailedException>(() => ticketTask);

        Assert.AreEqual(SteamAuthTicketRequestFailureKind.TicketLengthWasOutOfRange, failure.FailureKind);
        Assert.AreEqual(0, _steamAuthTicketApi.LiveSubscriptionCount);
        CollectionAssert.AreEqual(new[] { IssuedAuthTicketHandle }, _steamAuthTicketApi.CancelledAuthTicketHandles);
    }

    #endregion

    #region Case 9 - an unissuable ticket handle fails fast

    [Test]
    public void RequestAuthTicketHexForIdentityString_FailsFastWithTicketRequestCouldNotBeIssued_WhenSteamReturnsTheInvalidHandle()
    {
        _steamAuthTicketApi.AuthTicketHandleToIssue = SteamAuthTicketRequester.InvalidAuthTicketHandle;
        var requester = CreateRequesterThatShouldNotTimeOut();

        var failure = Assert.ThrowsAsync<SteamAuthTicketRequestFailedException>(
            () => requester.RequestAuthTicketHexForIdentityString(IdentityString)
        );

        Assert.AreEqual(SteamAuthTicketRequestFailureKind.TicketRequestCouldNotBeIssued, failure.FailureKind);
        Assert.AreEqual(
            0,
            _steamAuthTicketApi.LiveSubscriptionCount,
            "No response can ever arrive for an invalid handle, so no subscription may be left behind."
        );
    }

    #endregion

    #region Case 10 - a non OK result for our handle faults the task with the result code

    [Test]
    public void RequestAuthTicketHexForIdentityString_FailsWithSteamResultCode_WhenResponseForIssuedHandleIsNotOk()
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(new SteamAuthTicketResponse(
            IssuedAuthTicketHandle,
            NonOkSteamResultCode,
            CreateTicketBufferStartingWith(Array.Empty<byte>(), 0x00),
            0
        ));

        var failure = Assert.ThrowsAsync<SteamAuthTicketRequestFailedException>(() => ticketTask);

        Assert.AreEqual(SteamAuthTicketRequestFailureKind.SteamReportedNonOkResult, failure.FailureKind);
        Assert.AreEqual(NonOkSteamResultCode, failure.SteamResultCode);
        Assert.AreEqual(0, _steamAuthTicketApi.LiveSubscriptionCount);
        CollectionAssert.AreEqual(new[] { IssuedAuthTicketHandle }, _steamAuthTicketApi.CancelledAuthTicketHandles);
    }

    #endregion

    #region Case 11 - no response before the timeout throws and releases the ticket

    [Test]
    public void RequestAuthTicketHexForIdentityString_ThrowsTimeoutAndCancelsTheTicket_WhenNoResponseArrivesInTime()
    {
        var requester = CreateRequesterThatTimesOutImmediately();

        Assert.ThrowsAsync<TimeoutException>(() => requester.RequestAuthTicketHexForIdentityString(IdentityString));

        CollectionAssert.AreEqual(new[] { IssuedAuthTicketHandle }, _steamAuthTicketApi.CancelledAuthTicketHandles);
        Assert.AreEqual(1, _steamAuthTicketApi.TotalSubscriptionCount);
        Assert.AreEqual(0, _steamAuthTicketApi.LiveSubscriptionCount);
    }

    #endregion

    #region Case 12 - every terminal path disposes the subscription

    // The success, non OK result, timeout and invalid handle terminal paths assert
    // LiveSubscriptionCount == 0 inside cases 1, 6, 8, 9, 10 and 11. This test covers the
    // remaining terminal path: cancellation while the response is still outstanding.
    [Test]
    public void RequestAuthTicketHexForIdentityString_DisposesSubscriptionAndCancelsTicket_WhenCancellationIsRequestedWhileWaiting()
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        using (var cancellationTokenSource = new CancellationTokenSource())
        {
            var ticketTask = requester.RequestAuthTicketHexForIdentityString(
                IdentityString,
                cancellationTokenSource.Token
            );

            cancellationTokenSource.Cancel();

            Assert.CatchAsync<OperationCanceledException>(() => ticketTask);
        }

        Assert.AreEqual(0, _steamAuthTicketApi.LiveSubscriptionCount);
        CollectionAssert.AreEqual(new[] { IssuedAuthTicketHandle }, _steamAuthTicketApi.CancelledAuthTicketHandles);
    }

    #endregion

    #region Case 13 - responses for other handles are ignored

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_LeavesTaskPendingAndSubscriptionAlive_WhenResponseCarriesAForeignHandle()
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(CreateOkResponseCarryingTicketBytes(ForeignAuthTicketHandle, 0xC0, 0xFF, 0xEE));

        Assert.IsFalse(ticketTask.IsCompleted);
        Assert.AreEqual(1, _steamAuthTicketApi.LiveSubscriptionCount);

        DeliverResponse(CreateOkResponseCarryingTicketBytes(IssuedAuthTicketHandle, 0x01));
        await ticketTask;
    }

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_ResolvesWithMatchingTicket_WhenForeignHandleResponseArrivesFirst()
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(CreateOkResponseCarryingTicketBytes(ForeignAuthTicketHandle, 0xC0, 0xFF, 0xEE));
        DeliverResponse(CreateOkResponseCarryingTicketBytes(IssuedAuthTicketHandle, 0xBE, 0xEF));

        Assert.AreEqual("BEEF", await ticketTask);
    }

    #endregion

    #region Case 14 - a duplicate response for our handle does not throw

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_DoesNotThrow_WhenTwoResponsesArriveForTheIssuedHandle()
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketTask = requester.RequestAuthTicketHexForIdentityString(IdentityString);
        DeliverResponse(CreateOkResponseCarryingTicketBytes(IssuedAuthTicketHandle, 0x11, 0x22));

        Assert.DoesNotThrow(
            () => DeliverResponse(CreateOkResponseCarryingTicketBytes(IssuedAuthTicketHandle, 0x33, 0x44)),
            "Completing the same request twice must be tolerated rather than throwing."
        );

        Assert.AreEqual("1122", await ticketTask);
        Assert.AreEqual(0, _steamAuthTicketApi.LiveSubscriptionCount);
    }

    #endregion

    #region Case 15 - a response delivered during the request call still completes the task

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_Completes_WhenResponseIsDeliveredSynchronouslyDuringTheRequestCall()
    {
        _steamAuthTicketApi.RespondSynchronouslyDuringTicketRequest = issuedAuthTicketHandle =>
            DeliverResponse(CreateOkResponseCarryingTicketBytes(issuedAuthTicketHandle, 0x5A));

        var requester = CreateRequesterThatShouldNotTimeOut();

        var ticketHex = await requester.RequestAuthTicketHexForIdentityString(IdentityString);

        Assert.AreEqual("5A", ticketHex);
        Assert.AreEqual(0, _steamAuthTicketApi.LiveSubscriptionCount);
    }

    #endregion

    #region Case 16 - overlapping requests and pre cancelled tokens

    [Test]
    public async Task RequestAuthTicketHexForIdentityString_CompletesEachOverlappingRequestWithItsOwnHandlesResponse()
    {
        const uint firstAuthTicketHandle = 100u;
        const uint secondAuthTicketHandle = 200u;
        _steamAuthTicketApi.EnqueueAuthTicketHandlesToIssue(firstAuthTicketHandle, secondAuthTicketHandle);

        var requester = CreateRequesterThatShouldNotTimeOut();

        var firstTicketTask = requester.RequestAuthTicketHexForIdentityString("first-identity");
        var secondTicketTask = requester.RequestAuthTicketHexForIdentityString("second-identity");

        Assert.AreEqual(2, _steamAuthTicketApi.LiveSubscriptionCount);

        DeliverResponse(CreateOkResponseCarryingTicketBytes(secondAuthTicketHandle, 0x22));

        Assert.AreEqual("22", await secondTicketTask);
        Assert.IsFalse(firstTicketTask.IsCompleted);

        DeliverResponse(CreateOkResponseCarryingTicketBytes(firstAuthTicketHandle, 0x11));

        Assert.AreEqual("11", await firstTicketTask);
        Assert.AreEqual(0, _steamAuthTicketApi.LiveSubscriptionCount);
    }

    [Test]
    public void RequestAuthTicketHexForIdentityString_ThrowsWithoutRequestingATicket_WhenCancellationTokenIsAlreadyCancelled()
    {
        var requester = CreateRequesterThatShouldNotTimeOut();

        using (var alreadyCancelledTokenSource = new CancellationTokenSource())
        {
            alreadyCancelledTokenSource.Cancel();

            Assert.CatchAsync<OperationCanceledException>(
                () => requester.RequestAuthTicketHexForIdentityString(
                    IdentityString,
                    alreadyCancelledTokenSource.Token
                )
            );
        }

        Assert.AreEqual(0, _steamAuthTicketApi.TicketRequestCount);
        Assert.AreEqual(0, _steamAuthTicketApi.LiveSubscriptionCount);
        CollectionAssert.IsEmpty(_steamAuthTicketApi.CancelledAuthTicketHandles);
    }

    #endregion

    #region Helpers

    private SteamAuthTicketRequester CreateRequesterThatShouldNotTimeOut()
    {
        return new SteamAuthTicketRequester(_steamAuthTicketApi, TicketResponseTimeoutThatShouldNeverElapse);
    }

    private SteamAuthTicketRequester CreateRequesterThatTimesOutImmediately()
    {
        return new SteamAuthTicketRequester(_steamAuthTicketApi, TicketResponseTimeoutThatElapsesImmediately);
    }

    private void DeliverResponse(SteamAuthTicketResponse response)
    {
        _steamAuthTicketApi.DeliverResponseToEveryLiveSubscription(response);
    }

    private static SteamAuthTicketResponse CreateOkResponseCarryingTicketBytes(
        uint authTicketHandle,
        params byte[] meaningfulTicketBytes
    )
    {
        return new SteamAuthTicketResponse(
            authTicketHandle,
            SteamAuthTicketResponse.ResultCodeOk,
            CreateTicketBufferStartingWith(meaningfulTicketBytes, 0x00),
            meaningfulTicketBytes.Length
        );
    }

    /// <summary>
    /// Steam always marshals the full fixed size buffer, so every response in these tests carries one
    /// too, with everything past the meaningful bytes set to <paramref name="trailingFillerByte"/>.
    /// </summary>
    private static byte[] CreateTicketBufferStartingWith(byte[] leadingBytes, byte trailingFillerByte)
    {
        var ticketBuffer = new byte[SteamAuthTicketResponse.TicketBufferLength];

        for (var index = 0; index < ticketBuffer.Length; index++)
        {
            ticketBuffer[index] = index < leadingBytes.Length
                ? leadingBytes[index]
                : trailingFillerByte;
        }

        return ticketBuffer;
    }

    private static byte[] CreateTicketBufferFilledWithARepeatingPattern()
    {
        var ticketBuffer = new byte[SteamAuthTicketResponse.TicketBufferLength];

        for (var index = 0; index < ticketBuffer.Length; index++)
            ticketBuffer[index] = (byte)(index % 256);

        return ticketBuffer;
    }

    private static string RenderExpectedUppercaseHex(byte[] ticketBuffer, int meaningfulTicketLength)
    {
        var expectedHex = new StringBuilder(meaningfulTicketLength * 2);

        for (var index = 0; index < meaningfulTicketLength; index++)
            expectedHex.Append(ticketBuffer[index].ToString("X2", CultureInfo.InvariantCulture));

        return expectedHex.ToString();
    }

    #endregion
}
