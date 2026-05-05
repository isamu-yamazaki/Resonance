using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Resonance.Assemblies.LobbySystem;
using UnityEngine;

namespace Resonance.LobbySystem
{
    /// <summary>
    /// Use for development only.
    /// Controls a server (bound to all local interfaces) which implements basic
    /// functionality required for a lobby provider. Some functionality off the
    /// critical path (e.g. friends) are not implemented and should be stubbed by
    /// the provider instead.
    /// </summary>
    public class DummyLobbyServer
    {
        // Thread-safe RNG for lobby ids. CreateLobby runs on a thread-pool task,
        // so UnityEngine.Random is unsafe here.
        private static readonly System.Random _idRandom = new System.Random();
        private const int LobbyIdMin = 10_000_000;   // 8 digits
        private const int LobbyIdMax = 100_000_000;  // exclusive

        private List<Lobby> lobbies;

        private HttpListener httpListener;

        public void AttemptStart(string portNumber)
        {
            lobbies = new List<Lobby>();

            httpListener = new HttpListener();
            // "+" binds to all hostnames on every interface. HttpListener does
            // not accept raw IPs (e.g. 0.0.0.0). On Windows this requires a URL
            // ACL reservation: `netsh http add urlacl url=http://+:<port>/ user=Everyone`.
            // Mono on macOS/Linux accepts it without elevation.
            httpListener.Prefixes.Add($"http://+:{portNumber}/api/");
            try
            {
                httpListener.Start();
            }
            catch (HttpListenerException ex)
            {
                Debug.LogError($"[DummyLobbyServer] Failed to bind to http://+:{portNumber}/api/: {ex.Message}. On Windows, run: netsh http add urlacl url=http://+:{portNumber}/ user=Everyone");
                httpListener = null;
                return;
            }

            Listen();
        }

        internal static string GenerateLobbyId(IList<Lobby> existing)
        {
            lock (_idRandom)
            {
                for (int attempt = 0; attempt < 16; attempt++)
                {
                    string candidate = _idRandom.Next(LobbyIdMin, LobbyIdMax).ToString();
                    bool collides = false;
                    if (existing != null)
                    {
                        for (int i = 0; i < existing.Count; i++)
                        {
                            if (existing[i].LobbyId == candidate)
                            {
                                collides = true;
                                break;
                            }
                        }
                    }
                    if (!collides)
                    {
                        return candidate;
                    }
                }
                throw new InvalidOperationException("Failed to allocate a unique lobby id after 16 attempts");
            }
        }

        private void Listen()
        {
            Task serverTask = Task.Run(async () =>
            {
                while (httpListener != null)
                {
                    HttpListenerContext context = await httpListener.GetContextAsync();
                    await ProcessRequestAsync(context);
                }
            });

        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            var method = request.HttpMethod;
            var rawUrl = request.RawUrl ?? "";

            var lobbyUserIdMatch = Regex.Match(rawUrl, @"^.*/lobby/(\d+)/users/(\d+)(?:/|$)$");
            if (lobbyUserIdMatch.Success)
            {
                int lobbyId = int.Parse(lobbyUserIdMatch.Groups[1].Value);
                int userId = int.Parse(lobbyUserIdMatch.Groups[2].Value);

                await HandleLobbyIdUserIdEndpoint(lobbyId, userId, method, request, response);
                return;
            }

            var lobbyUsersMatch = Regex.Match(rawUrl, @"^.*/lobby/(\d+)/users(?:/|$)");
            if (lobbyUsersMatch.Success)
            {
                int lobbyId = int.Parse(lobbyUsersMatch.Groups[1].Value);

                await HandleLobbyIdUserEndpoint(lobbyId, method, request, response);
                return;
            }

            var lobbyMatch = Regex.Match(rawUrl, @"^.*/lobby/(\d+)(?:/|$)$");
            if (lobbyMatch.Success)
            {
                int lobbyId = int.Parse(lobbyMatch.Groups[1].Value);

                await HandleLobbyIdEndpoint(lobbyId, method, request, response);
                return;
            }

            if (rawUrl.EndsWith("/lobby") || rawUrl.EndsWith("/lobby/"))
            {
                await HandleLobbyEndpoint(method, request, response);
                return;
            }

            await HandleNotFound(response);
        }

        private async Task HandleLobbyEndpoint(string method, HttpListenerRequest request, HttpListenerResponse response)
        {
            switch (method)
            {
                case "POST":
                    await CreateLobby(request, response);
                    break;
                case "GET":
                    await ListLobbies(response);
                    break;
                default:
                    await WriteErrorResponse(response, HttpStatusCode.MethodNotAllowed, "Method not allowed");
                    break;
            }
        }

        private async Task HandleLobbyIdEndpoint(int lobbyId, string method, HttpListenerRequest request, HttpListenerResponse response)
        {
            switch (method)
            {
                case "GET":
                    await GetLobby(lobbyId, response);
                    break;
                case "PUT":
                    await UpdateLobby(lobbyId, request, response);
                    break;
                default:
                    await WriteErrorResponse(response, HttpStatusCode.MethodNotAllowed, "Method not allowed");
                    break;
            }
        }

        private async Task UpdateLobby(int lobbyId, HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var lobbyIndex = lobbies.FindIndex(l => l.LobbyId == lobbyId.ToString());

                if (lobbyIndex == -1)
                {
                    await WriteErrorResponse(response, HttpStatusCode.NotFound, "Lobby not found");
                    return;
                }

                string requestBody = await ReadRequestBody(request);
                var lobbyData = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);

                if (lobbyData != null)
                {
                    if (lobbyData.ContainsKey("UnderlyingProviderProperties"))
                    {
                        var lobbyPropertiesData = JsonConvert.DeserializeObject<Dictionary<string, string>>(lobbyData["UnderlyingProviderProperties"].ToString());
                        foreach (var prop in lobbyPropertiesData)
                        {
                            lobbies[lobbyIndex].UnderlyingProviderProperties[prop.Key] = prop.Value?.ToString();
                        }
                    }
                }

                await WriteJsonResponse(response, lobbies[lobbyIndex]);
            }
            catch (Exception ex)
            {
                await WriteErrorResponse(response, HttpStatusCode.BadRequest, "Failed to update lobby: " + ex.Message);
            }
        }

        private async Task HandleLobbyIdUserEndpoint(int lobbyId, string method, HttpListenerRequest request, HttpListenerResponse response)
        {
            switch (method)
            {
                case "POST":
                    await JoinLobby(lobbyId, request, response);
                    break;
                case "GET":
                    await ListLobbyMembers(lobbyId, response);
                    break;
                default:
                    await WriteErrorResponse(response, HttpStatusCode.MethodNotAllowed, "Method not allowed");
                    break;
            }
        }

        private async Task HandleLobbyIdUserIdEndpoint(int lobbyId, int userId, string method, HttpListenerRequest request, HttpListenerResponse response)
        {
            switch (method)
            {
                case "PUT":
                    await UpdateUserInLobby(lobbyId, userId, request, response);
                    break;
                case "DELETE":
                    await LeaveLobby(lobbyId, userId.ToString(), response);
                    break;
                default:
                    await WriteErrorResponse(response, HttpStatusCode.MethodNotAllowed, "Method not allowed");
                    break;
            }
        }

        private async Task WriteJsonResponse(HttpListenerResponse response, object data)
        {
            string json = JsonConvert.SerializeObject(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.StatusCode = (int)HttpStatusCode.OK;

            using (Stream output = response.OutputStream)
            {
                await output.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        private async Task WriteErrorResponse(HttpListenerResponse response, HttpStatusCode statusCode, string message)
        {
            var errorResponse = new { error = message };
            string json = JsonConvert.SerializeObject(errorResponse);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.StatusCode = (int)statusCode;

            using (Stream output = response.OutputStream)
            {
                await output.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        private async Task<string> ReadRequestBody(HttpListenerRequest request)
        {
            using (Stream body = request.InputStream)
            using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private async Task CreateLobby(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                string requestBody = await ReadRequestBody(request);
                var lobbyData = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);

                int maxPlayers = lobbyData != null && lobbyData.ContainsKey("maxPlayers")
                    ? Convert.ToInt32(lobbyData["maxPlayers"])
                    : 4;

                string lobbyId = GenerateLobbyId(lobbies);

                string lobbyName = lobbyData != null && lobbyData.ContainsKey("name")
                    ? lobbyData["name"].ToString()
                    : "Dummy Lobby " + lobbyId;

                var newLobby = new Lobby
                {
                    LobbyId = lobbyId,
                    Name = lobbyName,
                    MaxPlayers = maxPlayers,
                    IsValid = true,
                    UnderlyingProviderProperties = new Dictionary<string, string>(),
                    Members = new List<LobbyUser>()
                };

                lobbies.Add(newLobby);

                await WriteJsonResponse(response, newLobby);
            }
            catch (Exception ex)
            {
                await WriteErrorResponse(response, HttpStatusCode.BadRequest, "Failed to create lobby: " + ex.Message);
            }
        }

        private async Task ListLobbies(HttpListenerResponse response)
        {
            try
            {
                await WriteJsonResponse(response, lobbies);
            }
            catch (Exception ex)
            {
                await WriteErrorResponse(response, HttpStatusCode.InternalServerError, "Failed to list lobbies: " + ex.Message);
            }
        }

        private async Task GetLobby(int lobbyId, HttpListenerResponse response)
        {
            try
            {
                var lobby = lobbies.Find(l => l.LobbyId == lobbyId.ToString());

                if (lobby.LobbyId == null)
                {
                    await WriteErrorResponse(response, HttpStatusCode.NotFound, "Lobby not found");
                    return;
                }

                await WriteJsonResponse(response, lobby);
            }
            catch (Exception ex)
            {
                await WriteErrorResponse(response, HttpStatusCode.InternalServerError, "Failed to get lobby: " + ex.Message);
            }
        }

        private async Task JoinLobby(int lobbyId, HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var lobbyIndex = lobbies.FindIndex(l => l.LobbyId == lobbyId.ToString());

                if (lobbyIndex == -1)
                {
                    await WriteErrorResponse(response, HttpStatusCode.NotFound, "Lobby not found");
                    return;
                }

                string requestBody = await ReadRequestBody(request);
                var userData = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);

                string userId = userData != null && userData.ContainsKey("UserId")
                    ? userData["UserId"].ToString()
                    : "User" + lobbies[lobbyIndex].Members.Count;

                string displayName = userData != null && userData.ContainsKey("DisplayName")
                    ? userData["DisplayName"].ToString()
                    : "User " + userId;

                var existingMemberIndex = lobbies[lobbyIndex].Members.FindIndex(m => m.Id == userId);
                if (existingMemberIndex == -1)
                {
                    bool isFirstMember = lobbies[lobbyIndex].Members.Count == 0;
                    lobbies[lobbyIndex].Members.Add(new LobbyUser
                    {
                        Id = userId,
                        DisplayName = displayName,
                        IsReady = false,
                        IsOwner = isFirstMember
                    });
                }

                await WriteJsonResponse(response, lobbies[lobbyIndex]);
            }
            catch (Exception ex)
            {
                await WriteErrorResponse(response, HttpStatusCode.BadRequest, "Failed to join lobby: " + ex.Message);
            }
        }

        private async Task ListLobbyMembers(int lobbyId, HttpListenerResponse response)
        {
            try
            {
                var lobbyIndex = lobbies.FindIndex(l => l.LobbyId == lobbyId.ToString());
                if (lobbyIndex == -1)
                {
                    await WriteErrorResponse(response, HttpStatusCode.NotFound, "Lobby not found");
                    return;
                }
                await WriteJsonResponse(response, lobbies[lobbyIndex].Members);
            }
            catch (Exception ex)
            {
                await WriteErrorResponse(response, HttpStatusCode.InternalServerError, "Failed to list lobby members: " + ex.Message);
            }
        }

        private async Task UpdateUserInLobby(int lobbyId, int userId, HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var lobbyIndex = lobbies.FindIndex(l => l.LobbyId == lobbyId.ToString());

                if (lobbyIndex == -1)
                {
                    await WriteErrorResponse(response, HttpStatusCode.NotFound, "Lobby not found");
                    return;
                }

                string requestBody = await ReadRequestBody(request);
                var updateData = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);

                var memberIndex = lobbies[lobbyIndex].Members.FindIndex(m => m.Id == userId.ToString());

                if (memberIndex == -1)
                {
                    await WriteErrorResponse(response, HttpStatusCode.NotFound, "User not found in lobby");
                    return;
                }

                if (updateData.ContainsKey("IsReady"))
                {
                    var member = lobbies[lobbyIndex].Members[memberIndex];
                    member.IsReady = Convert.ToBoolean(updateData["IsReady"]);
                    lobbies[lobbyIndex].Members[memberIndex] = member;
                }

                await WriteJsonResponse(response, lobbies[lobbyIndex].Members[memberIndex]);
            }
            catch (Exception ex)
            {
                await WriteErrorResponse(response, HttpStatusCode.BadRequest, "Failed to update user: " + ex.Message);
            }
        }

        private async Task LeaveLobby(int lobbyId, string userId, HttpListenerResponse response)
        {
            try
            {
                var lobbyIndex = lobbies.FindIndex(l => l.LobbyId == lobbyId.ToString());
                if (lobbyIndex == -1)
                {
                    await WriteErrorResponse(response, HttpStatusCode.NotFound, "Lobby not found");
                    return;
                }

                var memberIndex = lobbies[lobbyIndex].Members.FindIndex(m => m.Id == userId);

                if (memberIndex == -1)
                {
                    await WriteErrorResponse(response, HttpStatusCode.NotFound, "User not found in lobby");
                    return;
                }

                lobbies[lobbyIndex].Members.RemoveAt(memberIndex);

                await WriteJsonResponse(response, new { success = true, message = "User left lobby successfully" });
            }
            catch (Exception ex)
            {
                await WriteErrorResponse(response, HttpStatusCode.InternalServerError, "Failed to leave lobby: " + ex.Message);
            }
        }

        private async Task HandleNotFound(HttpListenerResponse response)
        {
            // See https://www.zetcode.com/csharp/httplistener/
            response.Headers.Set("Content-Type", "text/plain");
            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.StatusDescription = "Endpoint not found";

            using Stream ros = response.OutputStream;
            string err = "404 - not found";

            byte[] ebuf = Encoding.UTF8.GetBytes(err);
            response.ContentLength64 = ebuf.Length;

            ros.Write(ebuf, 0, ebuf.Length);
        }

        public void Stop()
        {
            httpListener?.Stop();
            httpListener = null;
        }
    }
}
