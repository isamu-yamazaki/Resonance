using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// Turns Steam's callback-based web API auth ticket flow into a single awaitable operation.
    /// </summary>
    public class SteamAuthTicketRequester
    {
        /// <summary>Mirrors HAuthTicket.Invalid, which Steam returns when a request cannot be issued.</summary>
        public const uint InvalidAuthTicketHandle = 0u;

        public static readonly TimeSpan DefaultTicketResponseTimeout = TimeSpan.FromSeconds(10);

        private const string UppercaseHexDigits = "0123456789ABCDEF";

        private readonly ISteamAuthTicketApi _steamAuthTicketApi;
        private readonly TimeSpan _ticketResponseTimeout;

        public SteamAuthTicketRequester(ISteamAuthTicketApi steamAuthTicketApi)
            : this(steamAuthTicketApi, DefaultTicketResponseTimeout)
        {
        }

        public SteamAuthTicketRequester(
            ISteamAuthTicketApi steamAuthTicketApi,
            TimeSpan ticketResponseTimeout
        )
        {
            _steamAuthTicketApi = steamAuthTicketApi ?? throw new ArgumentNullException(nameof(steamAuthTicketApi));
            _ticketResponseTimeout = ticketResponseTimeout;
        }

        /// <summary>
        /// Requests a web API auth ticket for <paramref name="identityString"/> and resolves with the
        /// ticket rendered as uppercase hex with no separators.
        /// </summary>
        /// <exception cref="SteamAuthTicketRequestFailedException">
        /// Steam refused to issue a handle, reported a non-OK result, or returned an unusable ticket length.
        /// </exception>
        /// <exception cref="TimeoutException">No response arrived for the issued handle in time.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
        public async Task<string> RequestAuthTicketHexForIdentityString(
            string identityString,
            CancellationToken cancellationToken = default
        )
        {
            if (identityString == null)
                throw new ArgumentNullException(nameof(identityString));

            cancellationToken.ThrowIfCancellationRequested();

            var responseWaiter = new SteamAuthTicketResponseWaiter();

            // Subscribing before requesting is mandatory: Steam can deliver the response from inside
            // the request call itself, which a subscription opened afterwards would miss entirely.
            var responseSubscription = _steamAuthTicketApi.SubscribeToAuthTicketResponses(
                responseWaiter.OnAuthTicketResponseDelivered
            );
            responseWaiter.AttachResponseSubscription(responseSubscription);

            var issuedAuthTicketHandle = InvalidAuthTicketHandle;

            try
            {
                issuedAuthTicketHandle = _steamAuthTicketApi.RequestWebApiAuthTicket(identityString);

                if (issuedAuthTicketHandle == InvalidAuthTicketHandle)
                    throw new SteamAuthTicketRequestFailedException(
                        SteamAuthTicketRequestFailureKind.TicketRequestCouldNotBeIssued,
                        $"Steam refused to issue a web API auth ticket for identity string '{identityString}'."
                    );

                responseWaiter.AdoptIssuedAuthTicketHandle(issuedAuthTicketHandle);

                var response = await responseWaiter
                    .WaitForResponseForIssuedHandle(_ticketResponseTimeout, cancellationToken)
                    .ConfigureAwait(false);

                return RenderTicketAsUppercaseHex(response);
            }
            catch
            {
                // Only a ticket handed back to the caller may survive, because the orchestrator still
                // has to validate that one with Steam. Every other outcome abandons it.
                ReleaseIssuedAuthTicket(issuedAuthTicketHandle);
                throw;
            }
            finally
            {
                responseSubscription.Dispose();
            }
        }

        private void ReleaseIssuedAuthTicket(uint issuedAuthTicketHandle)
        {
            if (issuedAuthTicketHandle == InvalidAuthTicketHandle)
                return;

            _steamAuthTicketApi.CancelAuthTicket(issuedAuthTicketHandle);
        }

        private static string RenderTicketAsUppercaseHex(SteamAuthTicketResponse response)
        {
            if (!response.IsSuccessful)
                throw new SteamAuthTicketRequestFailedException(
                    SteamAuthTicketRequestFailureKind.SteamReportedNonOkResult,
                    $"Steam reported result code {response.ResultCode} for the requested web API auth ticket.",
                    response.ResultCode
                );

            var ticketBuffer = response.TicketBuffer;
            var meaningfulTicketLength = response.MeaningfulTicketLength;

            // Validated rather than trusted, so a nonsensical length from Steam can never escape as an
            // ArgumentOutOfRangeException from the conversion below.
            if (meaningfulTicketLength < 0 || meaningfulTicketLength > ticketBuffer.Length)
                throw new SteamAuthTicketRequestFailedException(
                    SteamAuthTicketRequestFailureKind.TicketLengthWasOutOfRange,
                    $"Steam reported a web API auth ticket length of {meaningfulTicketLength} bytes, " +
                    $"which does not fit inside the {ticketBuffer.Length} byte ticket buffer."
                );

            if (meaningfulTicketLength == 0)
                throw new SteamAuthTicketRequestFailedException(
                    SteamAuthTicketRequestFailureKind.TicketWasEmpty,
                    "Steam reported success but delivered a web API auth ticket with no meaningful bytes."
                );

            return ConvertToUppercaseHexWithoutSeparators(ticketBuffer, meaningfulTicketLength);
        }

        private static string ConvertToUppercaseHexWithoutSeparators(byte[] bytes, int byteCountToRender)
        {
            var hexCharacters = new char[byteCountToRender * 2];

            for (var byteIndex = 0; byteIndex < byteCountToRender; byteIndex++)
            {
                var byteToRender = bytes[byteIndex];
                hexCharacters[byteIndex * 2] = UppercaseHexDigits[byteToRender >> 4];
                hexCharacters[byteIndex * 2 + 1] = UppercaseHexDigits[byteToRender & 0x0F];
            }

            return new string(hexCharacters);
        }
    }
}
