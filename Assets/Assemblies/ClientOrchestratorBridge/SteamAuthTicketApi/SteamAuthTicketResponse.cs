using System;

namespace Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// Steamworks-free mirror of GetTicketForWebApiResponse_t so the auth ticket state machine
    /// and its tests never need a reference to the Steamworks assembly.
    /// </summary>
    public readonly struct SteamAuthTicketResponse
    {
        /// <summary>Mirrors EResult.k_EResultOK.</summary>
        public const int ResultCodeOk = 1;

        /// <summary>
        /// Steam always marshals the full fixed-size ticket buffer regardless of how many bytes
        /// are meaningful, so the buffer must always be sliced by <see cref="MeaningfulTicketLength"/>.
        /// </summary>
        public const int TicketBufferLength = 2560;

        public uint AuthTicketHandle { get; }
        public int ResultCode { get; }
        public byte[] TicketBuffer { get; }
        public int MeaningfulTicketLength { get; }

        public SteamAuthTicketResponse(
            uint authTicketHandle,
            int resultCode,
            byte[] ticketBuffer,
            int meaningfulTicketLength
        )
        {
            AuthTicketHandle = authTicketHandle;
            ResultCode = resultCode;
            TicketBuffer = ticketBuffer ?? Array.Empty<byte>();
            MeaningfulTicketLength = meaningfulTicketLength;
        }

        public bool IsSuccessful => ResultCode == ResultCodeOk;
    }
}
