using System;

namespace Assemblies.ClientOrchestratorBridge
{
    public class SteamAuthTicketRequestFailedException : Exception
    {
        public SteamAuthTicketRequestFailureKind FailureKind { get; }

        /// <summary>
        /// The raw Steam result code that accompanied the failure, when the failure originated from
        /// a delivered response. Null for failures that occur without a response.
        /// </summary>
        public int? SteamResultCode { get; }

        public SteamAuthTicketRequestFailedException(
            SteamAuthTicketRequestFailureKind failureKind,
            string message
        ) : base(message)
        {
            FailureKind = failureKind;
            SteamResultCode = null;
        }

        public SteamAuthTicketRequestFailedException(
            SteamAuthTicketRequestFailureKind failureKind,
            string message,
            int steamResultCode
        ) : base(message)
        {
            FailureKind = failureKind;
            SteamResultCode = steamResultCode;
        }
    }
}
