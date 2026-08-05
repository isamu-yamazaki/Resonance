using System;
using System.Collections;
using System.Net.Http;
using PurrNet;
using PurrNet.Transports;
using Resonance.Assemblies.ClientOrchestratorBridge;
using Resonance.Assemblies.LobbySystem;
using Resonance.Assemblies.ServerOrchestratorBridge;
using Resonance.BuildTools;
using Resonance.Contracts;
using UnityEngine;

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

            if (ShouldStartAsServer)
            {
                StartCoroutine(StartServer());
            }
            else
            {
                StartCoroutine(StartClient());
            }
        }

        private IEnumerator StartServer()
        {
            var client = new HttpClient();
            var envVars = EnvironmentVariablesReceiver.Instance;
            if (envVars.OrchestratorUrl == null)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. No orchestrator URL available.",
                    this);
                yield break;
            }

            if (envVars.MatchId == null || envVars.MatchKey == null)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. No match ID or match key available.",
                    this);
                yield break;
            }

            if (envVars.GameServerPort == 0 || envVars.GameServerPort == null)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. No game server port passed.",
                    this);
                yield break;
            }

            client.BaseAddress = new Uri(envVars.OrchestratorUrl);
            var bridge = new ServerOrchestratorBridge(client, envVars.MatchId, envVars.MatchKey);

            var readyTask = bridge.SignalAsReady();
            yield return new WaitUntil(() => readyTask.IsCompleted);

            if (readyTask.Exception != null)
            {
                // TODO: add orchestrator handling
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to join the match: {readyTask.Exception}", this);
                yield break;
            }

            var getMembersTask = bridge.GetMembers();
            yield return new WaitUntil(() => getMembersTask.IsCompleted);
            if (getMembersTask.Exception != null)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to join the match: {getMembersTask.Exception}",
                    this);
                yield break;
            }

            if (getMembersTask.Result == null)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to join the match; the server did not return the expected result.",
                    this);
                yield break;
            }

            // TODO: store members
            var members = getMembersTask.Result;

            _transport.serverPort = envVars.GameServerPort.Value;
            _networkManager.StartServer();
        }

        private IEnumerator StartClient()
        {
            // TODO: apparently there's a better way to implement this using Awaitable

            // client side still requires lobby data holder
            if (!_lobbyDataHolder)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. {nameof(LobbyDataHolder)} is null!",
                    this);
                yield break;
            }

            if (!_lobbyDataHolder.CurrentLobby.IsValid)
            {
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to start connection. Lobby is invalid!", this);
                yield break;
            }

            if (!HasClientConfig) yield break;

            var config = ClientBuildConfigReceiver.Instance.Config;

            var client = new HttpClient();
            client.BaseAddress = new Uri(config.orchestratorUrl);
            var bridge = ClientOrchestratorBridge.BuildWithPlatform(
                config.enableSteamLobby ? Platform.Steam : Platform.Dummy,
                client
            );

            var getMatchDtoTask = bridge.GetJoinMatchDtoForLobby(
                _lobbyDataHolder.CurrentLobby
            );
            yield return new WaitUntil(() => getMatchDtoTask.IsCompleted);
            var joinMatchDto = getMatchDtoTask.Result;

            var joinMatchTask = bridge.JoinMatch(joinMatchDto);
            yield return new WaitUntil(() => getMatchDtoTask.IsCompleted);
            if (joinMatchTask.Exception != null)
            {
                // TODO: add UI handling
                Debug.LogError(
                    $"[{nameof(OrchestratorConnectionStarter)}] Failed to join the match: {joinMatchTask.Exception}",
                    this);
                yield break;
            }
            else if (joinMatchTask.Result == null)
            {
                Debug.Log($"[{nameof(OrchestratorConnectionStarter)}] Failed to join the match: the server did not return the expected result.", this);
                yield break;
            }

            var joinMatchResult = joinMatchTask.Result;

            _transport.address = joinMatchResult.DedicatedServerHost;
            _transport.serverPort = (ushort)joinMatchResult.DedicatedServerPort;

            ClientTokenHolder.Instance?.SetClientToken(joinMatchResult.ServerAuthToken);

            _networkManager.StartClient();
        }

        #region Build Context

        private bool HasClientConfig => ClientBuildConfigReceiver.Instance != null;
        private bool HasServerConfig => ServerBuildConfigReceiver.Instance != null;
        private bool IsLobbyOwner => _lobbyDataHolder.CurrentLobby.IsOwner(_lobbyDataHolder.LocalUserId);

        #endregion

        #region Outcome Predicates

        private bool ShouldStartAsServer => HasServerConfig;

        #endregion
    }
}