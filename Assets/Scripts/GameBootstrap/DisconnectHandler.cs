using System;
using System.Linq;
using System.Threading.Tasks;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resonance.GameBootstrap
{
    public class DisconnectHandler : MonoBehaviour
    {
        private ConnectionState _previousState = ConnectionState.Disconnected;
        private NetworkManager _networkManager;

        /// <summary>
        /// The scene to load into on network shutdown.
        /// </summary>
        [SerializeField] private string disconnectedScene;

        /// <summary>
        /// If the active scene is one of these scenes, the disconnect handler will not
        /// attempt to load the "disconnected" scene.
        /// </summary>
        [SerializeField] private string[] noDisconnectScenes;

        private void Awake()
        {
            if (!TryGetComponent(out _networkManager))
            {
                Debug.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
            }
        }

        private void Start()
        {
            Debug.Log("[DisconnectHandler] Disconnect handler started");
            _networkManager.onClientConnectionState += OnClientConnectionState;
        }


        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.onClientConnectionState -= OnClientConnectionState;
            }
        }

        private async void OnClientConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Disconnected && _previousState != ConnectionState.Disconnected)
            {
                Debug.Log($"[{nameof(DisconnectHandler)}] Attempting to disconnect");
                await TryDisconnect();
            }
            _previousState = state;
        }

        private async Task TryDisconnect()
        {
            if (!noDisconnectScenes.Contains(SceneManager.GetActiveScene().name))
            {
                await Task.Delay(1000);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene(disconnectedScene);
            }
        }
    }
}
