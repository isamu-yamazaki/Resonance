using PurrNet;
using Resonance.LobbySystem.DataProviders;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class PlayerSkinApplicator : NetworkBehaviour
    {
        [SerializeField] private PlayerSkinRenderer skinRenderer;

        private SkinIndexProvider _skinIndexProvider;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            if (!isOwner) { return; }

            _skinIndexProvider = SkinIndexProvider.Instance;
            if (_skinIndexProvider == null) { return; }

            skinRenderer.RequestSkin(_skinIndexProvider.SkinIndex);
            _skinIndexProvider.OnSkinIndexChanged.AddListener(OnSkinIndexChanged);
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            if (_skinIndexProvider != null)
            {
                _skinIndexProvider.OnSkinIndexChanged.RemoveListener(OnSkinIndexChanged);
            }
        }

        private void OnSkinIndexChanged(int index) => skinRenderer.RequestSkin(index);
    }
}
