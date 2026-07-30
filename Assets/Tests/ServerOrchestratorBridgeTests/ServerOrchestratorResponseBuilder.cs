using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Resonance.Contracts;

public static class ServerOrchestratorResponseBuilder
{
    private const string JsonMediaType = "application/json";

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
}