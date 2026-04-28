using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Resonance.Assemblies.LobbySystem;
using UnityEngine;
using UnityEngine.Events;

namespace Resonance.LobbySystem
{
    /// <summary>
    /// Use for development only.
    /// An in-memory lobby provider which contains no external dependencies.
    /// The first client that is run starts an instance of DummyLobbyServer
    /// on the specified port. Other clients connect to the instance started
    /// by the first client. Not all functionality is implemented (e.g. friends).
    /// </summary>
    public class DummyLobbyProvider : MonoBehaviour, ILobbyProvider
    {
        class Content : HttpContent
        {
            private readonly string _data;
            public Content(string data)
            {
                _data = data;
            }

            // Minimal implementation needed for an HTTP request content,
            // i.e. a content that will be sent via HttpClient, contains the 2 following methods.
            protected override bool TryComputeLength(out long length)
            {
                // This content doesn't support pre-computed length and
                // the request will NOT contain Content-Length header.
                length = 0;
                return false;
            }

            // SerializeToStream* methods are internally used by CopyTo* methods
            // which in turn are used to copy the content to the NetworkStream.
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
                => stream.WriteAsync(Encoding.UTF8.GetBytes(_data)).AsTask();
        }

        public event UnityAction<string> OnLobbyJoinFailed;
        public event UnityAction OnLobbyLeft;
        public event UnityAction<Lobby> OnLobbyUpdated;
        public event UnityAction<List<LobbyUser>> OnLobbyPlayerListUpdated;
        public event UnityAction<List<FriendUser>> OnFriendListPulled;
#pragma warning disable CS0067
        public event UnityAction<string> OnFriendInviteAccepted;
#pragma warning restore CS0067
        public event UnityAction<string> OnError;

        [SerializeField] private string portNumber = "5001";

        private DummyLobbyServer server;
        private HttpClient client;
        private string currentLobbyId;
        private string localUserId;
        private Coroutine updateCoroutine;
        private static WaitForSeconds coroutineWaitForSeconds = new WaitForSeconds(2f);

        private async void Start()
        {
            // Generate a local user ID if we don't have one
            localUserId = "" + UnityEngine.Random.Range(1000, 9999);

            bool serverRunning = await CheckServerRunning();

            if (!serverRunning)
            {
                server = new DummyLobbyServer();
                server.AttemptStart(portNumber);
            }

            client = new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{portNumber}")
            };

            updateCoroutine = StartCoroutine(PeriodicLobbyUpdate());
        }


        private async Task<bool> CheckServerRunning()
        {
            try
            {
                using (var testClient = new HttpClient())
                {
                    testClient.Timeout = TimeSpan.FromSeconds(1);
                    var response = await testClient.GetAsync($"http://localhost:{portNumber}/api/lobby");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<Lobby> GetLobbyDataFullAsync()
        {
            if (string.IsNullOrEmpty(currentLobbyId))
            {
                throw new InvalidOperationException("No current lobby");
            }

            var response = await client.GetAsync($"api/lobby/{currentLobbyId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to get lobby data: " + response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync();
            return Lobby.FromJson(content);
        }

        private async Task<Lobby> RefreshLobbyDataAndTriggerUpdate()
        {
            if (string.IsNullOrEmpty(currentLobbyId))
            {
                return new Lobby { IsValid = false };
            }

            try
            {
                var lobbyResponse = await client.GetAsync($"api/lobby/{currentLobbyId}");
                if (!lobbyResponse.IsSuccessStatusCode)
                {
                    return new Lobby { IsValid = false };
                }

                var lobbyContent = await lobbyResponse.Content.ReadAsStringAsync();
                var result = Lobby.FromJson(lobbyContent);

                OnLobbyUpdated?.Invoke(result);
                return result;
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Failed to refresh lobby data: " + ex.Message);
                return new Lobby { IsValid = false };
            }
        }

        private IEnumerator PeriodicLobbyUpdate()
        {
            while (true)
            {
                yield return coroutineWaitForSeconds;

                RefreshLobbyDataAndTriggerUpdate().ConfigureAwait(false);
            }
        }


        private void OnApplicationQuit()
        {
            server?.Stop();
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
        }

        public async Task<Lobby> CreateLobbyAsync(int maxPlayers, Dictionary<string, string> lobbyProperties = null)
        {
            try
            {
                var lobbyData = new
                {
                    maxPlayers = maxPlayers,
                    name = "Dummy Lobby",
                    properties = lobbyProperties ?? new Dictionary<string, string>()
                };

                string jsonData = JsonConvert.SerializeObject(lobbyData);
                var response = await client.PostAsync("api/lobby", new Content(jsonData));

                if (!response.IsSuccessStatusCode)
                {
                    OnError?.Invoke("Failed to create lobby: " + response.StatusCode);
                    return new Lobby { IsValid = false };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var lobby = Lobby.FromJson(responseContent);

                if (string.IsNullOrEmpty(lobby.LobbyId))
                {
                    return new Lobby { IsValid = false };
                }

                currentLobbyId = lobby.LobbyId;

                await JoinLobbyAsync(lobby.LobbyId);

                return await RefreshLobbyDataAndTriggerUpdate();
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Failed to create lobby: " + ex.Message);
                return new Lobby { IsValid = false };
            }
        }

        public async Task<List<FriendUser>> GetFriendsAsync(LobbyManager.FriendFilter filter)
        {
            return new List<FriendUser>();
        }

        public async Task<string> GetLobbyDataAsync(string key)
        {
            if (string.IsNullOrEmpty(currentLobbyId))
            {
                OnError?.Invoke("No current lobby");
                return null;
            }

            try
            {
                var response = await client.GetAsync($"api/lobby/{currentLobbyId}");

                if (!response.IsSuccessStatusCode)
                {
                    OnError?.Invoke("Failed to get lobby data: " + response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var lobby = Lobby.FromJson(content);

                if (lobby.UnderlyingProviderProperties != null && lobby.UnderlyingProviderProperties.TryGetValue(key, out string value))
                {
                    return value;
                }

                return null;
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Failed to get lobby data: " + ex.Message);
                return null;
            }
        }

        public async Task<List<LobbyUser>> GetLobbyMembersAsync()
        {
            if (string.IsNullOrEmpty(currentLobbyId))
            {
                OnError?.Invoke("No current lobby");
                return new List<LobbyUser>();
            }

            try
            {
                var lobby = await GetLobbyDataFullAsync();
                var members = lobby.Members ?? new List<LobbyUser>();

                OnLobbyPlayerListUpdated?.Invoke(members);
                return members;
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Failed to get lobby members: " + ex.Message);
                return new List<LobbyUser>();
            }
        }

        public Task<string> GetLocalUserIdAsync()
        {
            return Task.FromResult(localUserId);
        }

        public Task<string> GetLocalDisplayNameAsync()
        {
            return Task.FromResult("Player " + localUserId);
        }

        public Task<Texture2D> GetLocalAvatarAsync()
        {
            return Task.FromResult<Texture2D>(null);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task InviteFriendAsync(FriendUser user)
        {
            return Task.CompletedTask;
        }

        public async Task<Lobby> JoinLobbyAsync(string lobbyId)
        {
            try
            {
                var lobbyResponse = await client.GetAsync($"api/lobby/{lobbyId}");
                if (!lobbyResponse.IsSuccessStatusCode)
                {
                    OnLobbyJoinFailed?.Invoke("Failed to get lobby information");
                    return new Lobby { IsValid = false };
                }

                var lobbyContent = await lobbyResponse.Content.ReadAsStringAsync();
                var lobby = Lobby.FromJson(lobbyContent);

                if (string.IsNullOrEmpty(lobby.LobbyId))
                {
                    OnLobbyJoinFailed?.Invoke("Lobby not found");
                    return new Lobby { IsValid = false };
                }

                // Join the lobby
                var joinData = new { UserId = localUserId, DisplayName = "Player " + localUserId };
                string jsonData = JsonConvert.SerializeObject(joinData);
                var joinResponse = await client.PostAsync($"api/lobby/{lobbyId}/users", new Content(jsonData));

                if (!joinResponse.IsSuccessStatusCode)
                {
                    OnLobbyJoinFailed?.Invoke("Failed to join lobby: " + joinResponse.StatusCode);
                    return new Lobby { IsValid = false };
                }

                currentLobbyId = lobbyId;

                return await RefreshLobbyDataAndTriggerUpdate();
            }
            catch (Exception ex)
            {
                OnLobbyJoinFailed?.Invoke("Failed to join lobby: " + ex.Message);
                return new Lobby { IsValid = false };
            }
        }

        public async Task LeaveLobbyAsync()
        {
            if (string.IsNullOrEmpty(currentLobbyId))
            {
                OnError?.Invoke("No current lobby to leave");
                return;
            }

            await LeaveLobbyAsync(currentLobbyId);

        }

        public async Task LeaveLobbyAsync(string lobbyId)
        {
            try
            {
                var response = await client.DeleteAsync($"api/lobby/{lobbyId}/users/{localUserId}");

                if (response.IsSuccessStatusCode)
                {
                    if (lobbyId == currentLobbyId)
                    {
                        currentLobbyId = null;
                        OnLobbyLeft?.Invoke();
                    }
                }
                else
                {
                    OnError?.Invoke("Failed to leave lobby: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Failed to leave lobby: " + ex.Message);
            }
        }

        public async Task<List<Lobby>> SearchLobbiesAsync(int maxRoomsToFind = 10, Dictionary<string, string> filters = null)
        {
            try
            {
                var response = await client.GetAsync("api/lobby");

                if (!response.IsSuccessStatusCode)
                {
                    return new List<Lobby>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Lobby>>(content);
                return result ?? new List<Lobby>();
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Failed to search lobbies: " + ex.Message);
                return new List<Lobby>();
            }
        }

        public async Task SetAllReadyAsync()
        {
            if (string.IsNullOrEmpty(currentLobbyId))
            {
                OnError?.Invoke("No current lobby");
                return;
            }

            try
            {
                // Get all members and set them to ready
                var members = await GetLobbyMembersAsync();
                foreach (var member in members)
                {
                    await SetIsReadyAsync(member.Id, true);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Failed to set all ready: " + ex.Message);
            }
        }

        public async Task SetIsReadyAsync(string userId, bool isReady)
        {
            if (string.IsNullOrEmpty(currentLobbyId))
            {
                OnError?.Invoke("No current lobby");
                return;
            }

            try
            {
                var updateData = new { IsReady = isReady };
                string jsonData = JsonConvert.SerializeObject(updateData);
                var response = await client.PutAsync($"api/lobby/{currentLobbyId}/users/{userId}", new Content(jsonData));

                if (!response.IsSuccessStatusCode)
                {
                    OnError?.Invoke("Failed to set ready state: " + response.StatusCode);
                    return;
                }

                // Refresh lobby data to trigger update event
                await RefreshLobbyDataAndTriggerUpdate();
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Failed to set ready state: " + ex.Message);
            }
        }

        public async Task SetLobbyDataAsync(string key, string value)
        {
            if (string.IsNullOrEmpty(currentLobbyId))
            {
                OnError?.Invoke("No current lobby");
                return;
            }

            try
            {
                var lobbyData = await GetLobbyDataFullAsync();
                if (lobbyData.UnderlyingProviderProperties == null)
                {
                    lobbyData.UnderlyingProviderProperties = new Dictionary<string, string>();
                }

                lobbyData.UnderlyingProviderProperties[key] = value;

                string jsonData = JsonConvert.SerializeObject(lobbyData);
                var response = await client.PutAsync($"api/lobby/{currentLobbyId}", new Content(jsonData));

                if (!response.IsSuccessStatusCode)
                {
                    OnError?.Invoke("Failed to set lobby data: " + response.StatusCode);
                }
                else
                {
                    // Refresh lobby data after update
                    await RefreshLobbyDataAndTriggerUpdate();
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Failed to set lobby data: " + ex.Message);
            }
        }

        public Task SetLobbyStartedAsync()
        {
            return Task.CompletedTask;
        }

        public void Shutdown()
        {
            server?.Stop();
            client?.Dispose();
            currentLobbyId = null;
        }
    }
}
