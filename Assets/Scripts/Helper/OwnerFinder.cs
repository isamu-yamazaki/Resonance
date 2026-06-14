using System.Linq;
using PurrNet;
using PurrNet.Prediction;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Helper
{
    public class OwnerFinder
    {
        public static GameObject FindGameObjectOfOwnedPlayerPredictedController()
        {
            return FindFirstOwnedPredictedObjectByType<PlayerPredictedController>()?.gameObject;
        }

        public static T FindFirstOwnedNetworkObjectByType<T>() where T : NetworkIdentity
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None).FirstOrDefault(p => p.isOwner);
        }

        public static T FindFirstOwnedPredictedObjectByType<T>() where T : PredictedIdentity
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None).FirstOrDefault(p => p.isOwner);
        }

        /// <summary>
        /// Returns true if <paramref name="hit"/> belongs to the player identified by <paramref name="owner"/>.
        /// Walks up to the PlayerPredictedController (colliders live on child objects, but ownership sits on
        /// the player root) and compares PurrNet ownership by PlayerID. Returns false for non-player objects
        /// (e.g. walls) and when <paramref name="owner"/> has no value, so callers proceed normally in those
        /// cases. Replay-safe: ownership is stable networked state and the parent walk is deterministic, so
        /// this is safe to call inside a Simulate pass.
        /// </summary>
        public static bool BelongsToOwner(Component hit, PlayerID? owner)
        {
            if (hit == null || !owner.HasValue)
                return false;

            PlayerPredictedController controller = hit.GetComponentInParent<PlayerPredictedController>();
            return controller != null && controller.owner == owner;
        }

        /// <summary>
        /// Returns the GameObject of the PlayerPredictedController owned by <paramref name="player"/>,
        /// or null if no value is supplied or no matching player is found. Scans the active
        /// PlayerPredictedControllers and matches on PurrNet ownership by PlayerID.
        /// </summary>
        public static GameObject FindPlayerGameObjectById(PlayerID? player)
        {
            if (!player.HasValue)
                return null;

            return Object.FindObjectsByType<PlayerPredictedController>(FindObjectsSortMode.None)
                .FirstOrDefault(p => p.owner == player)?.gameObject;
        }
    }
}
