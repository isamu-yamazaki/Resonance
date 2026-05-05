using System.Linq;
using PurrNet;
using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Helper
{
    public class OwnerFinder
    {
        public static T FindFirstOwnedNetworkObjectByType<T>() where T : NetworkIdentity
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None).FirstOrDefault(p => p.isOwner);
        }

        public static T FindFirstOwnedPredictedObjectByType<T>() where T : PredictedIdentity
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None).FirstOrDefault(p => p.isOwner);
        }
    }
}
