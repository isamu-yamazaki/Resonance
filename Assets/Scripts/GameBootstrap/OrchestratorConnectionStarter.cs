using System;
using System.Net.Http;
using System.Threading.Tasks;
using PurrNet;
using PurrNet.Transports;
using Resonance.Assemblies.ClientOrchestratorBridge;
using Resonance.Assemblies.LobbySystem;
using Resonance.Assemblies.ServerOrchestratorBridge;
using Resonance.BuildTools;
using Resonance.Contracts;
using UnityEngine;
using UnityEngine.Serialization;

#if UTP_LOBBYRELAY
using PurrNet.UTP;
using Unity.Services.Relay.Models;
#endif

namespace Resonance.GameBootstrap
{
    /// <summary>
    /// Orchestrator-based connection starter for both the client and server paths.
    /// </summary>
    public class OrchestratorConnectionStarter : MonoBehaviour
    {
        private NetworkManager _networkManager;
        private LobbyDataHolder _lobbyDataHolder;
        private UDPTransport _transport;

        /// <summary>
        /// The number of seconds to delay for when joining as a client in the editor.
        /// This gives time to set the match ID and key when using the server flow in-editor.
        /// </summary>
        [FormerlySerializedAs("startDelaySeconds")] [SerializeField]
        private double editorStartDelaySeconds = 30;

        private void Awake()
        {
            if (!TryGetComponent(out _networkManager))
            {
                Debug.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
            }

            _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (!_lobbyDataHolder)
                Debug.LogError($"Failed to get {nameof(LobbyDataHolder)} component.", this);
        }

        private void Start()
        {
            if (EnvironmentVariablesReceiver.Instance == null)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. {nameof(EnvironmentVariablesReceiver)} does not exist!",
                    this);
                return;
            }

            if (!_networkManager)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. {nameof(NetworkManager)} is null!",
                    this);
                return;
            }

            if (_networkManager.transport is UDPTransport transport)
            {
                _transport = transport;
            }
            else
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. Only {nameof(UDPTransport)} is supported.",
                    this);
                return;
            }

            _ = ShouldStartAsServer ? StartServer() : StartClient();
        }

        private async Task StartServer()
        {
            var client = new HttpClient();
            var envVars = EnvironmentVariablesReceiver.Instance;
            if (envVars.OrchestratorUrl == null)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. No orchestrator URL available.",
                    this);
                return;
            }

            if (envVars.MatchId == null || envVars.MatchKey == null)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. No match ID or match key available.",
                    this);
                return;
            }

            if (envVars.GameServerPort is 0 or null)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. No game server port passed.",
                    this);
                return;
            }

            if (ServerBuildConfigReceiver.Instance is null)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. No ServerBuildConfigReceiver instance.");
                return;
            }

            var config = ServerBuildConfigReceiver.Instance.Config;
            if (config.intendedServerVersion == null)
            {
                Debug.Log(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. No `intendedServerVersion` set in build config.");
                return;
            }

            if (envVars.IntendedServerVersion != config.intendedServerVersion)
            {
                Debug.Log(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. Expected server version {config.intendedServerVersion} from config, but got {envVars.IntendedServerVersion} from environment.");
                return;
            }

            client.BaseAddress = new Uri(envVars.OrchestratorUrl);
            var bridge = new ServerOrchestratorBridge(client, envVars.MatchId, envVars.MatchKey);

            try
            {
                var members = await bridge.GetMembers();

                _transport.serverPort = envVars.GameServerPort.Value;
                _networkManager.StartServer();

                // wait before assigning data to a networked object
                await Task.Delay(1000);


                var dataHolder = FindFirstObjectByType<NetworkedMatchDataHolder>();
                if (dataHolder)
                {
                    dataHolder.SetMembers(members);
                }
                else
                {
                    Debug.LogError(
                        $"[{nameof(OrchestratorConnectionStarter)}] Failed to join the match; {nameof(NetworkedMatchDataHolder)} is null!",
                        this);

                    _networkManager.StopServer();
                }

                // members must be set before this is called
                await bridge.SignalAsReady();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to join the match: {e}", this);
            }
        }

        private async Task StartClient()
        {
            // client side still requires lobby data holder
            if (!_lobbyDataHolder)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. {nameof(LobbyDataHolder)} is null!",
                    this);
                return;
            }

            if (!_lobbyDataHolder.CurrentLobby.IsValid)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. Lobby is invalid!", this);
                return;
            }

            if (!HasClientConfig) return;

            var config = ClientBuildConfigReceiver.Instance.Config;
            var intendedServerVersion = config.intendedServerVersion;

            var client = new HttpClient();
            client.BaseAddress = new Uri(config.orchestratorUrl);
            var bridge = ClientOrchestratorBridge.BuildWithPlatform(
                config.enableSteamLobby ? Platform.Steam : Platform.Dummy,
                client
            );

            try
            {
                var joinMatchDto = await bridge.GetJoinMatchDtoForLobby(
                    _lobbyDataHolder.CurrentLobby,
                    _lobbyDataHolder.LocalUserId,
                    intendedServerVersion
                );

                var joinMatchTask = bridge.JoinMatch(joinMatchDto);
                var joinMatchResult = await joinMatchTask;
                if (joinMatchResult == null)
                {
                    Debug.Log(
                        $"[{nameof(OrchestratorConnectionStarter)}] Failed to join the match: the server did not return the expected result.",
                        this);
                    return;
                }

#if UNITY_EDITOR
                Debug.Log(
                    $"[{nameof(OrchestratorConnectionStarter)}] Orchestrator join successful, configuring network manager");
#endif

                _transport.address = joinMatchResult.DedicatedServerHost;
                _transport.serverPort = (ushort)joinMatchResult.DedicatedServerPort;

                ClientTokenHolder.Instance?.SetClientToken(joinMatchResult.ServerAuthToken);

#if UNITY_EDITOR
                Debug.Log(
                    $"[{nameof(OrchestratorConnectionStarter)}] Editor detected, starting client in {editorStartDelaySeconds} seconds");
                await Task.Delay(TimeSpan.FromSeconds(editorStartDelaySeconds));
                Debug.Log($"[{nameof(OrchestratorConnectionStarter)}] Starting client");
#endif

                _networkManager.StartClient();
            }
            catch (Exception e)
            {
                // TODO: add UI handling
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to join the match: {e}",
                    this);
            }
        }

        #region Build Context

        private bool HasClientConfig => ClientBuildConfigReceiver.Instance != null;
        private bool HasServerConfig => ServerBuildConfigReceiver.Instance != null;

        #endregion

        #region Outcome Predicates

        private bool ShouldStartAsServer => HasServerConfig;

        #endregion
    }
}