using TMPro;
using UnityEngine;

// To be replaced with map selection UI, potentially
namespace Resonance.LobbySystem.TemporaryUI
{
    public class UpdateSceneNameButton : MonoBehaviour
    {
        [SerializeField] private TMP_InputField sceneNameInput;
        [SerializeField] private LobbyManager lobbyManager;

        public void UpdateSceneName()
        {
            if (string.IsNullOrEmpty(sceneNameInput.text))
            {
                Debug.LogWarning($"Can't start join, room ID is empty.");
                return;
            }

            lobbyManager.SetSceneNameOnLobby(sceneNameInput.text);
        }
    }
}
