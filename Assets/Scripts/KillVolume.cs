using UnityEngine;
using PurrNet;
using Resonance.Player;
using System.Collections;
using System.Collections.Generic;

public class KillVolume : NetworkBehaviour
{
    private HashSet<PlayerStats> _inside = new HashSet<PlayerStats>();

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (!other.TryGetComponent<PlayerStats>(out var stats)) return;
        if (_inside.Contains(stats)) return;

        _inside.Add(stats);
        stats.OnPlayerRespawn += () => OnPlayerRespawned(stats);
        stats.TakeDamage(999999f, null);
    }

    private void OnPlayerRespawned(PlayerStats stats)
    {
        stats.OnPlayerRespawn -= () => OnPlayerRespawned(stats);
        StartCoroutine(DelayedRemove(stats));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;
        if (other.TryGetComponent<PlayerStats>(out var stats))
            _inside.Remove(stats);
    }

    private IEnumerator DelayedRemove(PlayerStats stats)
    {
        yield return new WaitForSeconds(1f);
        _inside.Remove(stats);
    }
}
