using TMPro;
using UnityEngine;

namespace Resonance.LobbySystem.TemporaryUI
{
    public class ModeButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private LobbyManager lobbyManager;

        public void Start()
        {
            UpdateModeText();
        }

        public void UpdateModeText()
        {
            modeText.text = lobbyManager.CurrentLobby.GameMode.ToString();
        }
    }
}
