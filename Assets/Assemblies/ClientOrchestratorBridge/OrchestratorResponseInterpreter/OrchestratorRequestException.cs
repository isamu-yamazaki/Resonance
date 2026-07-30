using System;
using System.Net;

namespace Resonance.Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// The orchestrator answered with an unexpected status code or body.
    /// </summary>
    /// <remarks>
    /// Also see <see cref="JoinMatchFailedException"/>, which accounts for expected failure cases.
    /// </remarks>
    public class OrchestratorRequestException : Exception
    {
        public OrchestratorRequestException(
            string message,
            HttpStatusCode statusCode,
            string responseBody,
            Exception innerException = null
        ) : base(message, innerException)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody ?? string.Empty;
        }

        public HttpStatusCode StatusCode { get; }

        /// The response body verbatim.
        /// A bodiless response is
        /// recorded as <see cref="string.Empty"/>.
        public string ResponseBody { get; }
    }
}