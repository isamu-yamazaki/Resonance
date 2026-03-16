using UnityEngine;

namespace Resonance.BuildTools
{
    public class PurrTransportConfigurator : MonoBehaviour
    {
        public static BuildConfig Current { get; private set; }

        [SerializeField] BuildConfig config;
        [SerializeField] GameObject remoteTransport;
        [SerializeField] GameObject localTransport;

        void Awake()
        {
            Current = config;
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
