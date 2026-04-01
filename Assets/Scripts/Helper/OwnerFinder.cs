using System.Linq;
using PurrNet;
using UnityEngine;

namespace Resonance.Helper
{
    public class OwnerFinder
    {
        public static T FindFirstOwnedObjectByType<T>() where T : NetworkIdentity
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None).FirstOrDefault(p => p.isOwner);
        }
    }
}
