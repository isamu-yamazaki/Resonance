using UnityEngine;
using PurrNet;
using Resonance.Player;

public class KillVolume : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.TryGetComponent<PlayerStats>(out var stats))
            stats.TakeDamage(999999f, null);
    }
}
