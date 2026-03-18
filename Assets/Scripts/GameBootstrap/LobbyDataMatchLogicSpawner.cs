using PurrNet;
using Resonance.LobbySystem;
using Resonance.Match;
using UnityEngine;
using UnityEngine.Events;

namespace Resonance.GameBootstrap
{
    public class LobbyDataMatchLogicSpawner : NetworkBehaviour
    {
        [SerializeField] private GameObject matchLogicPrefab;

        public UnityEvent OnMatchLogicSpawned = new();

        private bool matchLogicSpawned = false;

        public void SpawnMatchLogic()
        {
            if (matchLogicSpawned)
            {
                Debug.Log($"[{GetType()}] Match logic already spawned for object {id}");
                return;
            }

            Debug.Log($"[{GetType()}] Spawning match logic for object {id}");

            // must be spawned on all clients to access RPC
            Instantiate(matchLogicPrefab);

            OnMatchLogicSpawned.Invoke();
            matchLogicSpawned = true;
        }
    }
}
