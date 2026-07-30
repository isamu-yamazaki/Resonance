using System;
using System.Net;
using Resonance.Contracts;

namespace Resonance.Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// Deliberate failure cases coming from the orchestrator.
    /// </summary>
    /// <remarks>
    /// Also see <see cref="OrchestratorRequestException"/>.
    /// </remarks>
    public class JoinMatchFailedException : Exception
    {
        public JoinMatchFailedException(
            JoinFailureReason reason,
            int joinedCount,
            int expectedCount,
            HttpStatusCode statusCode,
            TimeSpan? retryAfter
        ) : base(BuildMessageFromFailureFields(reason, joinedCount, expectedCount, statusCode))
        {
            Reason = reason;
            JoinedCount = joinedCount;
            ExpectedCount = expectedCount;
            StatusCode = statusCode;
            RetryAfter = retryAfter;
        }

        public JoinFailureReason Reason { get; }

        public int JoinedCount { get; }

        public int ExpectedCount { get; }

        public HttpStatusCode StatusCode { get; }

        /// <remarks>
        /// Populated from the <c>Retry-After</c> response header when the orchestrator sends one
        /// (it does for 503); null whenever the header is absent or unreadable.
        /// </remarks>
        public TimeSpan? RetryAfter { get; }

        private static string BuildMessageFromFailureFields(
            JoinFailureReason reason,
            int joinedCount,
            int expectedCount,
            HttpStatusCode statusCode
        )
        {
            return $"The orchestrator refused the join with {reason} ({(int)statusCode}); " +
                   $"{joinedCount} of {expectedCount} expected players had joined.";
        }
    }
}
