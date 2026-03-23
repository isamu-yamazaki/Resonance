using Resonance.Assemblies.LobbySystem;
using UnityEngine;

namespace Resonance.GameBootstrap
{
    public class DevelopmentGameModeProvider : GameModeProvider
    {
        [SerializeField] private GameMode gameModeToSet;

        private void Awake()
        {
            var existing = FindFirstObjectByType<GameModeProvider>();
            if (existing != null && existing != this)
            {
                Destroy(this);
                return;
            }

            gameMode = gameModeToSet;
        }
    }
}
