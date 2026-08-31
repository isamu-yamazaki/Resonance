namespace Assemblies.ClientOrchestratorBridge
{
    public enum SteamAuthTicketRequestFailureKind
    {
        /// <summary>Steam refused to issue a ticket handle, so no response will ever arrive.</summary>
        TicketRequestCouldNotBeIssued,

        /// <summary>Steam delivered a response for our handle carrying a non-OK result code.</summary>
        SteamReportedNonOkResult,

        /// <summary>Steam delivered an OK response whose ticket contained zero meaningful bytes.</summary>
        TicketWasEmpty,

        /// <summary>
        /// Steam delivered an OK response whose reported ticket length does not fit inside the
        /// fixed-size ticket buffer.
        /// </summary>
        TicketLengthWasOutOfRange
    }
}
