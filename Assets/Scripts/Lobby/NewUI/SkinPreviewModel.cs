using Resonance.LobbySystem.DataProviders;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.LobbySystem.NewUI
{
    public class SkinPreviewModel : MonoBehaviour
    {
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private SkinCatalog skinCatalog;
        [SerializeField, Range(0.1f, 1f)] private float resolutionScale = 0.5f;

        private SkinIndexProvider skinIndexProvider;

        private RenderTexture _rt;
        private GameObject _currentMesh;

        public RenderTexture PreviewTexture => _rt;

        private void Awake()
        {
            var width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * resolutionScale));
            var height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * resolutionScale));
            _rt = new RenderTexture(width, height, 16);
            previewCamera.targetTexture = _rt;
        }

        private void Start()
        {
            skinIndexProvider = SkinIndexProvider.Instance;
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
