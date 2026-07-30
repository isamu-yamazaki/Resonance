using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Resonance.Contracts;

namespace Resonance.Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// Turns what the orchestrator actually answered into either the DTO its endpoint promises or
    /// the exception that says why there is none.
    /// </summary>
    /// <remarks>
    /// No JSON failure ever escapes this type: an unreadable body is a request failure the caller
    /// can handle, not a parser exception leaking out of the bridge. Every exception it builds
    /// describes the response only — nothing from the outgoing request, whose authentication ticket
    /// must never reach a log.
    /// </remarks>
    internal static class OrchestratorResponseInterpreter
    {
        private const string FailureReasonPropertyName = "reason";
        private const string JoinedCountPropertyName = "joinedCount";
        private const string ExpectedCountPropertyName = "expectedCount";

        #region Reading the response

        /// <remarks>
        /// A response with no body reads as <see cref="string.Empty"/>, never null.
        /// </remarks>
        internal static async Task<string> ReadBodyOrEmpty(HttpResponseMessage response)
        {
            if (response.Content == null)
            {
                return string.Empty;
            }

            return await response.Content.ReadAsStringAsync() ?? string.Empty;
        }

        /// <remarks>
        /// Only the delta-seconds form is read, which is the form the orchestrator sends with its
        /// 503. An absent, date-shaped or unparseable header means no advice rather than an error.
        /// </remarks>
        internal static TimeSpan? ReadRetryAfterDelta(HttpResponseMessage response)
        {
            return response.Headers.RetryAfter?.Delta;
        }

        #endregion

        #region Interpreting the response

        internal static JoinMatchResultDto ReadJoinMatchResult(
            HttpStatusCode statusCode,
            string responseBody
        )
        {
            JoinMatchResultDto joinMatchResult;

            try
            {
                joinMatchResult = JsonConvert.DeserializeObject<JoinMatchResultDto>(responseBody);
            }
            catch (JsonException unreadableBody)
            {
                throw BuildUnreadableJoinResultOrchestratorRequestException(statusCode, responseBody, unreadableBody);
            }

            if (joinMatchResult == null)
            {
                throw BuildUnreadableJoinResultOrchestratorRequestException(statusCode, responseBody, null);
            }

            return joinMatchResult;
        }

        /// <summary>
        /// A refusal the orchestrator described in a <see cref="JoinFailureDto"/> becomes a
        /// <see cref="JoinMatchFailedException"/>; anything else becomes the generic request failure.
        /// </summary>
        internal static Exception InterpretUnsuccessfulJoinResponse(
            HttpStatusCode statusCode,
            string responseBody,
            TimeSpan? retryAfter
        )
        {
            if (IsDeliberateJoinRefusalStatus(statusCode)
                && TryConstructJoinFailureDtoManually(responseBody, out var joinFailure))
            {
                return new JoinMatchFailedException(
                    reason: joinFailure.Reason,
                    joinedCount: joinFailure.JoinedCount,
                    expectedCount: joinFailure.ExpectedCount,
                    statusCode: statusCode,
                    retryAfter: retryAfter
                );
            }

            return BuildUnexpectedResultOrchestratorRequestException(statusCode, responseBody);
        }

        internal static OrchestratorRequestException BuildUnexpectedResultOrchestratorRequestException(
            HttpStatusCode statusCode,
            string responseBody
        )
        {
            return new OrchestratorRequestException(
                $"The orchestrator answered {(int)statusCode} ({statusCode}) instead of the result its endpoint promises.",
                statusCode,
                responseBody
            );
        }

        /// <remarks>
        /// Only these two statuses carry a described refusal: 409 when the roster the orchestrator
        /// assembled disagrees with the request, 503 when it cannot host the match right now.
        /// </remarks>
        private static bool IsDeliberateJoinRefusalStatus(HttpStatusCode statusCode)
        {
            return statusCode is HttpStatusCode.Conflict or HttpStatusCode.ServiceUnavailable;
        }

        private static OrchestratorRequestException BuildUnreadableJoinResultOrchestratorRequestException(
            HttpStatusCode statusCode,
            string responseBody,
            Exception unreadableBody
        )
        {
            return new OrchestratorRequestException(
                $"The orchestrator answered {(int)statusCode} ({statusCode}) with a body that is not a {nameof(JoinMatchResultDto)}.",
                statusCode,
                responseBody,
                unreadableBody
            );
        }

        #endregion

        #region Reading a described join failure

        /// <remarks>
        /// Walked as a JSON tree rather than deserialized into the DTO because the deserializer
        /// answers the two questions that matter here wrongly: it throws on a reason string this
        /// client's contract does not know, and it binds a body with no reason at all to the default
        /// reason, reporting a refusal the orchestrator never sent.
        /// </remarks>
        private static bool TryConstructJoinFailureDtoManually(string responseBody, out JoinFailureDto joinFailure)
        {
            joinFailure = null;

            if (!TryParseJsonObject(responseBody, out var failureObject))
            {
                return false;
            }

            if (!TryReadRecognizedFailureReason(
                    failureObject.GetValue(FailureReasonPropertyName, StringComparison.OrdinalIgnoreCase),
                    out var reason
                ))
            {
                return false;
            }

            if (!TryReadCountOrZero(failureObject, JoinedCountPropertyName, out var joinedCount)
                || !TryReadCountOrZero(failureObject, ExpectedCountPropertyName, out var expectedCount))
            {
                return false;
            }

            joinFailure = new JoinFailureDto(
                reason: reason,
                joinedCount: joinedCount,
                expectedCount: expectedCount
            );

            return true;
        }

        private static bool TryParseJsonObject(string responseBody, out JObject parsedObject)
        {
            parsedObject = null;

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return false;
            }

            try
            {
                parsedObject = JToken.Parse(responseBody) as JObject;
            }
            catch (JsonException)
            {
                return false;
            }

            return parsedObject != null;
        }

        /// <remarks>
        /// A reason outside the client's contract is not a failure this client can reason about, so
        /// it is reported as unrecognized and degrades to the generic request failure.
        /// </remarks>
        private static bool TryReadRecognizedFailureReason(
            JToken reasonToken,
            out JoinFailureReason reason
        )
        {
            reason = default;

            if (reasonToken == null)
            {
                return false;
            }

            switch (reasonToken.Type)
            {
                case JTokenType.String:
                    return TryMatchFailureReasonName(reasonToken.Value<string>(), out reason);
                case JTokenType.Integer:
                    if (!TryReadInt32(reasonToken, out var reasonNumber))
                    {
                        return false;
                    }

                    reason = (JoinFailureReason)reasonNumber;
                    return Enum.IsDefined(typeof(JoinFailureReason), reason);
                case JTokenType.None:
                case JTokenType.Object:
                case JTokenType.Array:
                case JTokenType.Constructor:
                case JTokenType.Property:
                case JTokenType.Comment:
                case JTokenType.Float:
                case JTokenType.Boolean:
                case JTokenType.Null:
                case JTokenType.Undefined:
                case JTokenType.Date:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                default:
                    return false;
            }
        }

        /// <remarks>
        /// Matched against the contract's own names, case-insensitively like every other property
        /// read here, rather than with <c>Enum.TryParse</c>: that also accepts a numeric string and a
        /// comma-separated list of names, neither of which the orchestrator ever sends, and both of
        /// which can yield a defined reason it never described.
        /// </remarks>
        private static bool TryMatchFailureReasonName(string reasonName, out JoinFailureReason reason)
        {
            reason = default;

            if (string.IsNullOrEmpty(reasonName))
            {
                return false;
            }

            foreach (var definedReasonName in Enum.GetNames(typeof(JoinFailureReason)))
            {
                if (!string.Equals(definedReasonName, reasonName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                reason = (JoinFailureReason)Enum.Parse(typeof(JoinFailureReason), definedReasonName);
                return true;
            }

            return false;
        }

        /// <remarks>
        /// An absent or non-numeric count reads as zero, the way the DTO's own default does. A count
        /// the client cannot hold is a different thing: the orchestrator described a roster this
        /// client cannot represent, so the whole failure body is reported as unusable.
        /// </remarks>
        private static bool TryReadCountOrZero(JObject failureObject, string propertyName, out int count)
        {
            count = 0;

            var countToken = failureObject.GetValue(propertyName, StringComparison.OrdinalIgnoreCase);

            if (countToken == null || countToken.Type != JTokenType.Integer)
            {
                return true;
            }

            return TryReadInt32(countToken, out count);
        }

        /// <remarks>
        /// Read from the token's own value instead of through <c>Value&lt;int&gt;()</c>, which
        /// converts and throws for any JSON integer outside <see cref="int"/> range — an
        /// <see cref="OverflowException"/> that is not a <see cref="JsonException"/> and would
        /// escape this type. Newtonsoft carries a parsed JSON integer as a <c>long</c>, or as a
        /// <c>BigInteger</c> when it does not fit one; the latter is out of an <c>int</c>'s range by
        /// construction, so anything that is not a <c>long</c> reads as out of range.
        /// </remarks>
        private static bool TryReadInt32(JToken integerToken, out int value)
        {
            value = 0;

            if (integerToken is not JValue integerValue
                || !(integerValue.Value is long integerWithinInt64Range)
                || integerWithinInt64Range < int.MinValue
                || integerWithinInt64Range > int.MaxValue)
            {
                return false;
            }

            value = (int)integerWithinInt64Range;
            return true;
        }

        #endregion
    }
}
