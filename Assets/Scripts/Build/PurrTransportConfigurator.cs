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
            var shouldUseProductionRelay = false;
            if (receiver == null)
            {
                var serverReceiver = FindFirstObjectByType<ServerBuildConfigReceiver>();
                if (serverReceiver == null)
                {
                    Debug.LogError("[PurrTransportConfigurator] No ClientBuildConfigReceiver or ServerBuildConfigReceiver found in scene.");
                    return;
                }

                shouldUseProductionRelay = serverReceiver.Config.useProductionRelay;
            } else
            {
                shouldUseProductionRelay = receiver.Config.useProductionRelay;
            }

            if (localTransport != null)
            {
                localTransport.SetActive(!shouldUseProductionRelay);
            }
            if (remoteTransport != null)
            {
                remoteTransport.SetActive(shouldUseProductionRelay);
            }
        }
    }
}
