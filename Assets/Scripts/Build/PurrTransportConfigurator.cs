using UnityEngine;

namespace Resonance.BuildTools
{
    public class PurrTransportConfigurator : MonoBehaviour
    {
        [SerializeField] GameObject remoteTransport;
        [SerializeField] GameObject localTransport;

        void Awake()
        {
            var receiver = FindFirstObjectByType<ClientBuildConfigReceiver>();
            if (receiver == null)
            {
                Debug.LogError("[PurrTransportConfigurator] No ClientBuildConfigReceiver found in scene.");
                return;
            }

            var config = receiver.Config;
            if (localTransport != null)
            {
                localTransport.SetActive(!config.useProductionRelay);
            }
            if (remoteTransport != null)
            {
                remoteTransport.SetActive(config.useProductionRelay);
            }
        }
    }
}
