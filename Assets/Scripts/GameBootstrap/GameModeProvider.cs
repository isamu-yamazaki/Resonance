using Resonance.Assemblies.LobbySystem;
using UnityEngine;

namespace Resonance.GameBootstrap
{
    public abstract class GameModeProvider : MonoBehaviour
    {
        public GameMode gameMode { get; protected set; }

        protected virtual void Awake()
        {
            var existing = FindFirstObjectByType<GameModeProvider>();
            if (existing != null && existing != this)
            {
                Destroy(this);
                return;
            }

        }
    }
}
