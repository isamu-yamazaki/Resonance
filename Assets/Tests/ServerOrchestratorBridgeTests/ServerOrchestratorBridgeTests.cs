using System.Collections;
using System.Net;
using System.Net.Http;
using Assemblies.ServerOrchestratorBridge;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ServerOrchestratorBridgeTests
{
    private ServerOrchestratorBridge _bridge;
    private HttpClient _httpClient;
    private FakeHttpMessageHandler _httpHandler;

    #region SignalAsReady success

    [Test]
    public void SignalAsReady_CallsEndpointAndExits()
    {

    }

    #endregion

    #region SignalAsReady failures

    // Since we're not dealing with a DTO on the error cases,
    // we can return just one exception/check all the cases here

    [TestCase(HttpStatusCode.Conflict)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Gone)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public void SignalAsReady_ThrowsOnHttpErrorCodes(HttpStatusCode statusCode)
    {

    }

    #endregion

    #region GetMembers success

    [Test]
    public void GetMembers_ReturnsMembers()
    {

    }

    #endregion

    #region GetMembers failures

    [TestCase(HttpStatusCode.Conflict)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Gone)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public void GetMembers_ThrowsOnHttpErrorCodes(HttpStatusCode statusCode)
    {

    }

    #endregion

}
