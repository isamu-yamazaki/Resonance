using Resonance.LobbySystem;
using TMPro;
using UnityEngine;
using WebSocketSharp;

namespace Resonance.LobbySystem.TemporaryUI
{
    public class CurrentSceneName : MonoBehaviour
    {
        [SerializeField] private TMP_Text sceneNameText;
        [SerializeField] private LobbyManager lobbyManager;

        public void UpdateName()
        {
            var sceneName = lobbyManager.CurrentLobby.SceneName;
            if (sceneName.IsNullOrEmpty())
            {
                sceneNameText.text = "Scene: unknown";
            }
            else
            {
                sceneNameText.text = $"Scene: {sceneName}";
            }
        }
    }
}
