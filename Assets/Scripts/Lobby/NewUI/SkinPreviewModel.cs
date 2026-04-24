using System.Collections;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using Resonance.LobbySystem.DataProviders;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.LobbySystem.NewUI
{
    public class SkinPreviewModel : MonoBehaviour
    {
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform roomScreenCameraPose;
        [SerializeField] private Transform skinScreenCameraPose;
        [SerializeField] private SkinCatalog skinCatalog;
        [SerializeField, Range(0.1f, 1f)] private float resolutionScale = 1f;
        [SerializeField] private float cameraPoseTransitionDuration = 5f;

        private SkinIndexProvider skinIndexProvider;

        private RenderTexture _rt;
        private GameObject _currentMesh;
        private Coroutine _cameraPoseCoroutine;

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
                ShowOnlyRifleWeaponMesh(_currentMesh);
            }
        }

        private static void ShowOnlyRifleWeaponMesh(GameObject meshInstance)
        {
            var weaponMeshes = meshInstance.GetComponentsInChildren<TPWeaponMesh>(true);
            foreach (var weaponMesh in weaponMeshes)
            {
                weaponMesh.gameObject.SetActive(weaponMesh.weaponClass == WeaponClass.Rifle);
            }
        }

        public void ApplyRoomScreenCameraPose() => ApplyCameraPose(roomScreenCameraPose);

        public void ApplySkinScreenCameraPose() => ApplyCameraPose(skinScreenCameraPose);

        private void ApplyCameraPose(Transform pose)
        {
            if (!pose) return;
            if (_cameraPoseCoroutine != null)
            {
                StopCoroutine(_cameraPoseCoroutine);
            }
            _cameraPoseCoroutine = StartCoroutine(LerpCameraToPose(pose));
        }

        private IEnumerator LerpCameraToPose(Transform pose)
        {
            var camTransform = previewCamera.transform;
            var startPosition = camTransform.position;
            var startRotation = camTransform.rotation;
            var elapsed = 0f;

            while (elapsed < cameraPoseTransitionDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / cameraPoseTransitionDuration);
                var easedT = 1f - Mathf.Pow(1f - t, 3f);
                camTransform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, pose.position, easedT),
                    Quaternion.Slerp(startRotation, pose.rotation, easedT));
                yield return null;
            }

            camTransform.SetPositionAndRotation(pose.position, pose.rotation);
            _cameraPoseCoroutine = null;
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
