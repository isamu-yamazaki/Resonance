using Resonance.PlayerController;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.LobbySystem.TemporaryUI
{
    public class SkinPreviewRenderer : MonoBehaviour
    {
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private RawImage displayImage;
        [SerializeField] private SkinCatalog skinCatalog;
        [SerializeField] private Vector2Int renderSize = new(256, 256);

        private SkinIndexProvider skinIndexProvider;

        private RenderTexture _rt;
        private GameObject _currentMesh;

        private void Awake()
        {
            _rt = new RenderTexture(renderSize.x, renderSize.y, 16);
            previewCamera.targetTexture = _rt;
            displayImage.texture = _rt;
        }

        private void Start()
        {
            skinIndexProvider = FindFirstObjectByType<SkinIndexProvider>();
            if (!skinIndexProvider)
            {
                Debug.LogError($"[{GetType()}] No SkinIndexProvider object, cannot update render preview");
            }
            skinIndexProvider.OnSkinIndexChanged.AddListener(SetSkinIndex);
            SetSkinIndex(skinIndexProvider.SkinIndex);
        }

        public void SetSkinIndex(int index)
        {
            if (_currentMesh)
            {
                Destroy(_currentMesh);
            }

            var data = skinCatalog.Get(index);
            if (data?.bodyMeshPrefab)
            {
                _currentMesh = Instantiate(data.bodyMeshPrefab, spawnPoint);
            }
        }

        private void OnDestroy()
        {
            if (_rt)
            {
                _rt.Release();
            }
            skinIndexProvider.OnSkinIndexChanged.RemoveListener(SetSkinIndex);
        }
    }
}
