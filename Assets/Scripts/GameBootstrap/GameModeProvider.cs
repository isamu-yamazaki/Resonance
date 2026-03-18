using Resonance.LobbySystem;
using UnityEngine;

namespace Resonance.GameBootstrap
{
    public abstract class GameModeProvider : MonoBehaviour
    {
        public GameMode gameMode { get; protected set; }
    }
}
