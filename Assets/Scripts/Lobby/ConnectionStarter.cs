using System.Collections;
using PurrNet;
using PurrNet.Logging;
using PurrNet.Transports;
using Resonance.BuildTools;
using UnityEngine;

#if UTP_LOBBYRELAY
using PurrNet.UTP;
using Unity.Services.Relay.Models;
#endif

namespace Resonance.LobbySystem
{
    public class ConnectionStarter : MonoBehaviour
    {
        private NetworkManager _networkManager;
        private LobbyDataHolder _lobbyDataHolder;
        
        private void Awake()
        {
            if(!TryGetComponent(out _networkManager)) {
                PurrLogger.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
            }
            
            _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if(!_lobbyDataHolder)
                PurrLogger.LogError($"Failed to get {nameof(LobbyDataHolder)} component.", this);
        }

        private void Start()
        {
            if (!_networkManager)
            {
                PurrLogger.LogError($"Failed to start connection. {nameof(NetworkManager)} is null!", this);
                return;
            }
            
            if (!_lobbyDataHolder)
            {
                PurrLogger.LogError($"Failed to start connection. {nameof(LobbyDataHolder)} is null!", this);
                return;
            }
            
            if (!_lobbyDataHolder.CurrentLobby.IsValid)
            {
                PurrLogger.LogError($"Failed to start connection. Lobby is invalid!", this);
                return;
            }

            if(_networkManager.transport is PurrTransport) {
                (_networkManager.transport as PurrTransport).roomName = _lobbyDataHolder.CurrentLobby.LobbyId;
            } 
            
#if UTP_LOBBYRELAY
            else if(_networkManager.transport is UTPTransport) {
                if(_lobbyDataHolder.CurrentLobby.IsOwner(_lobbyDataHolder.LocalUserId)) {
                    (_networkManager.transport as UTPTransport).InitializeRelayServer((Allocation)_lobbyDataHolder.CurrentLobby.ServerObject);
                }
                (_networkManager.transport as UTPTransport).InitializeRelayClient(_lobbyDataHolder.CurrentLobby.Properties["JoinCode"]);
            }
#else
                // P2P Connection, receive IP/Port from server
#endif

            if (ShouldStartAsServer)
            {
                _networkManager.StartServer();
            }
            else if (ShouldStartAsHost)
            {
                _networkManager.StartHost();
            }
            else if (ShouldStartAsClient)
            {
                StartCoroutine(StartClient());
            }
            else
            {
                PurrLogger.LogError("Could not determine a network start mode. Check that a build config receiver is present in the scene.", this);
            }
        }

        #region Build Context

        private bool HasClientConfig => ClientBuildConfigReceiver.Instance != null;
        private bool HasServerConfig => ServerBuildConfigReceiver.Instance != null;
        private bool IsClientServerMode => HasClientConfig && ClientBuildConfigReceiver.Instance.Config.useClientServerMode;
        private bool IsHostMode => HasClientConfig && !ClientBuildConfigReceiver.Instance.Config.useClientServerMode;
        private bool IsLobbyOwner => _lobbyDataHolder.CurrentLobby.IsOwner(_lobbyDataHolder.LocalUserId);

        #endregion

        #region Outcome Predicates

        private bool ShouldStartAsServer => HasServerConfig;

        private bool ShouldStartAsHost => IsHostMode && IsLobbyOwner;

        private bool ShouldStartAsClient =>
            IsClientServerMode ||
            (IsHostMode && !IsLobbyOwner);

        #endregion

        private IEnumerator StartClient()
        {
            yield return new WaitForSeconds(1f);
            _networkManager.StartClient();
        }
    }
}
