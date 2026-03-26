using System;
using System.Linq;
using System.Threading.Tasks;
using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resonance.GameBootstrap
{
    public class DisconnectHandler : MonoBehaviour
    {
        private NetworkManager networkManager;

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
            if (!TryGetComponent(out networkManager))
            {
                Debug.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
            }
        }

        private void Start()
        {
            Debug.Log("[DisconnectHandler] Disconnect handler started");

            if (networkManager.isOffline)
            {
                Debug.Log("[DisconnectHandler] Network manager offline");
            }

            networkManager.onNetworkShutdownSimple += OnNetworkShutdown;
        }

        private void OnDestroy()
        {
            networkManager.onNetworkShutdownSimple -= OnNetworkShutdown;
        }

        private async void OnNetworkShutdown(NetworkManager manager)
        {
            if (!noDisconnectScenes.Contains(SceneManager.GetActiveScene().name))
            {
                await Task.Delay(1000);
                SceneManager.LoadScene(disconnectedScene);
            }
        }
    }
}
