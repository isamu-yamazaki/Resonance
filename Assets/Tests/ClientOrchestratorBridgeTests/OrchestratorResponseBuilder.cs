using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

/// <summary>
/// Builds responses shaped the way the real orchestrator shapes them: camelCase property names,
/// string enum values, and the exact media types and headers each status code carries. The bodies
/// are written as literal JSON rather than serialized from the contract DTOs on purpose — a test
/// that round-trips through the client's own serializer would pass even if the wire format drifted.
/// </summary>
public static class OrchestratorResponseBuilder
{
    public const string JsonMediaType = "application/json";
    public const string ProblemJsonMediaType = "application/problem+json";

    public static HttpResponseMessage WithoutBody(HttpStatusCode statusCode)
    {
        return new HttpResponseMessage(statusCode);
    }

    public static HttpResponseMessage WithBody(
        HttpStatusCode statusCode,
        string body,
        string mediaType = JsonMediaType
    )
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType)
        };
    }

    public static HttpResponseMessage JoinFailure(
        HttpStatusCode statusCode,
        string reason,
        int joinedCount,
        int expectedCount,
        TimeSpan? retryAfter = null
    )
    {
        var response = WithBody(
            statusCode,
            SerializeJoinFailureInServerWireFormat(reason, joinedCount, expectedCount)
        );

        if (retryAfter.HasValue)
        {
            response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter.Value);
        }

        return response;
    }

    public static string SerializeJoinFailureInServerWireFormat(
        string reason,
        int joinedCount,
        int expectedCount
    )
    {
        return $"{{\"reason\":\"{reason}\",\"joinedCount\":{joinedCount},\"expectedCount\":{expectedCount}}}";
    }

    public static string SerializeJoinMatchResultInServerWireFormat(
        Guid matchId,
        string dedicatedServerHost,
        int dedicatedServerPort,
        string serverAuthToken
    )
    {
        return $"{{\"matchId\":\"{matchId}\"," +
               $"\"dedicatedServerHost\":\"{dedicatedServerHost}\"," +
               $"\"dedicatedServerPort\":{dedicatedServerPort}," +
               $"\"serverAuthToken\":\"{serverAuthToken}\"}}";
    }

    /// <remarks>
    /// The orchestrator's 400 is an ASP.NET <c>ProblemDetails</c>, served as problem+json.
    /// </remarks>
    public static HttpResponseMessage ProblemDetails(
        HttpStatusCode statusCode,
        string title,
        string detail
    )
    {
        var body = $"{{\"type\":\"https://tools.ietf.org/html/rfc9110#section-15.5.1\"," +
                   $"\"title\":\"{title}\"," +
                   $"\"status\":{(int)statusCode}," +
                   $"\"detail\":\"{detail}\"}}";

        return WithBody(statusCode, body, ProblemJsonMediaType);
    }

    /// <remarks>
    /// The orchestrator's 401 body is a bare JSON string rather than an object, which is exactly
    /// the shape a naive <c>DeserializeObject&lt;TDto&gt;</c> blows up on.
    /// </remarks>
    public static HttpResponseMessage BareJsonStringBody(HttpStatusCode statusCode, string message)
    {
        return WithBody(statusCode, $"\"{message}\"");
    }
}
