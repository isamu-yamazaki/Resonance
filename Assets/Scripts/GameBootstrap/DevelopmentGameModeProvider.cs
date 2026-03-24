using Resonance.Assemblies.LobbySystem;
using UnityEngine;

namespace Resonance.GameBootstrap
{
    public class DevelopmentGameModeProvider : GameModeProvider
    {
        [SerializeField] private GameMode gameModeToSet;

        protected override void Awake()
        {
            base.Awake();
            gameMode = gameModeToSet;
        }
    }
}
