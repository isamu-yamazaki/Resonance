using UnityEngine;

namespace Resonance.LobbySystem
{
    [DefaultExecutionOrder(-1)]
    public class SkinIndexProviderSpawner : MonoBehaviour
    {
        public void Start()
        {
            var skinIndexProvider = FindFirstObjectByType<SkinIndexProvider>();
            if (!skinIndexProvider)
            {
                var newObject = new GameObject("SkinIndexProvider");
                newObject.AddComponent<SkinIndexProvider>();
                return;
            }
        }
    }
}
