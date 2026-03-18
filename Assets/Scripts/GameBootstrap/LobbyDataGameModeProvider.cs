using Resonance.LobbySystem;
using UnityEngine;

namespace Resonance.GameBootstrap
{
    public class LobbyDataGameModeProvider : GameModeProvider
    {
        private LobbyDataHolder lobbyDataHolder;

        private void Awake()
        {
            lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (lobbyDataHolder == null)
            {
                Debug.LogError($"[{GetType()}] Unable to find {nameof(LobbyDataHolder)} component");
                return;
            }

            gameMode = lobbyDataHolder.CurrentLobby.GameMode;
            Debug.Log($"[{GetType()}] Game mode set to {gameMode} from lobby data");
        }
    }
}
