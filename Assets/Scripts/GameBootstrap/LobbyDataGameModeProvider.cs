using Resonance.Assemblies.LobbySystem;
using Resonance.LobbySystem;
using UnityEngine;

namespace Resonance.GameBootstrap
{
    public class LobbyDataGameModeProvider : GameModeProvider
    {
        [SerializeField] private GameMode defaultGameModeIfNoLobby;
        private LobbyDataHolder lobbyDataHolder;

        protected override void Awake()
        {
            base.Awake();
            lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (lobbyDataHolder == null)
            {
                Debug.LogWarning($"[{GetType()}] Unable to find {nameof(LobbyDataHolder)} component, using default game mode");
                gameMode = defaultGameModeIfNoLobby;
                return;
            }

            gameMode = lobbyDataHolder.CurrentLobby.GameMode;
            Debug.Log($"[{GetType()}] Game mode set to {gameMode} from lobby data");
        }
    }
}
