using PurrNet;
using UnityEngine;
using UnityEngine.Events;

public class MatchLogicSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject matchLogicPrefab;

    public UnityEvent OnMatchLogicSpawned = new();

    private bool matchLogicSpawned = false;

    public void SpawnMatchLogic()
    {
        if (matchLogicSpawned)
        {
            Debug.Log($"[MatchLogicSpawner] Match logic already spawned for object {id}");
            return;
        }
        Debug.Log($"[MatchLogicSpawner] Spawning match logic for object {id}");

        // must be spawned on all clients to access RPC
        Instantiate(matchLogicPrefab);

        OnMatchLogicSpawned.Invoke();
        matchLogicSpawned = true;
    }
}
