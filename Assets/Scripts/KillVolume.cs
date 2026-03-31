using UnityEngine;
using PurrNet;
using Resonance.Player;
using System.Collections;
using System.Collections.Generic;

public class KillVolume : NetworkBehaviour
{
    private HashSet<PlayerStats> _inside = new HashSet<PlayerStats>();
    private float _respawnDelay = 3f;

    private void Start()
    {
        if (Respawn.Instance != null)
            _respawnDelay = Respawn.Instance.RespawnDelay + 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (!other.TryGetComponent<PlayerStats>(out var stats)) return;
        if (_inside.Contains(stats)) return;

        _inside.Add(stats);
        stats.TakeDamage(999999f, null);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;
        if (other.TryGetComponent<PlayerStats>(out var stats))
            StartCoroutine(DelayedRemove(stats));
    }

    private IEnumerator DelayedRemove(PlayerStats stats)
    {
        yield return new WaitForSeconds(_respawnDelay);
        _inside.Remove(stats);
    }
}
