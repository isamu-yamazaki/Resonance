using System;
using UnityEngine;

namespace Resonance.LobbySystem
{
    public class LobbyDataHolder : MonoBehaviour
    {
        [SerializeField] private Lobby serializedLobby;
        public Lobby CurrentLobby { get; private set; }

        public void SetCurrentLobby(Lobby newLobby)
        {
            CurrentLobby = newLobby;
            serializedLobby = newLobby;
        }
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
