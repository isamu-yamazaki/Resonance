using UnityEngine;

namespace Resonance.GameBootstrap
{
    public class ClientTokenHolder : MonoBehaviour
    {
        public static ClientTokenHolder Instance { get; private set; }

        public string ClientToken { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SetClientToken(string token)
        {
            ClientToken = token;
        }

        public void ClearClientToken()
        {
            ClientToken = null;
        }
    }
}