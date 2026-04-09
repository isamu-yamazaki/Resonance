using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Assemblies.SharedGameLogic;
using Resonance.Combat.Weapons.Enums;
using Resonance.Match;
using UnityEngine;
using UnityEngine.Rendering;

namespace Resonance.PlayerController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerSkinRenderer : NetworkBehaviour
    {
        [SerializeField] private SkinCatalog skinCatalog;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform fpArmsRoot;
        public Action<GameObject> OnNewSkinSpawned;

        public SyncVar<int> skinIndex = new SyncVar<int>();
        [SerializeField] private int testSkinIndexToRequest = 0;

        public GameObject CurrentMeshInstance { get; private set; }
        public SkinData CurrentlyLoadedSkinData { get; private set; }

        private GameObject _skillArmsInstance;
        public GameObject SkillArmsInstance => _skillArmsInstance;

        private GameObject _grappleArmsInstance;
        public GameObject GrappleArmsInstance => _grappleArmsInstance;

        private Dictionary<WeaponClass, GameObject> _fpArmsInstances = new Dictionary<WeaponClass, GameObject>();
        public IReadOnlyDictionary<WeaponClass, GameObject> FPArmsInstances => _fpArmsInstances;

        private bool _tpHidden;
        public bool IsTPHidden => _tpHidden;

        public bool ShouldRenderArmsOnlyBasedOnCachedMatchState
        {
            get
            {
                var roundManager = MatchLogicNetworkAdapter.Instance?.ActiveRoundManager;
                if (roundManager != null)
                {
                    if (roundManager.IsMatchActive)
                        return isOwner;
                    else
                        return false;
                }
                return isOwner;
            }
        }

        public async Task<bool> ShouldRenderArmsOnlyBasedOnAuthoritativeMatchState()
        {
            var roundManager = MatchLogicNetworkAdapter.Instance?.ActiveRoundManager;
            if (roundManager != null)
            {
                var matchState = await roundManager.GetMatchState();

                if ((BaseMatchState)matchState == BaseMatchState.MatchActive)
                    return isOwner;
                else
                    return false;
            }
            return isOwner;
        }

        private void Awake()
        {
            if (MatchLogicNetworkAdapter.Instance != null)
            {
                MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring += HandleFinishedConfiguring;

                if (MatchLogicNetworkAdapter.Instance.HasFinishedConfiguring)
                    HandleFinishedConfiguring();
            }
        }

        private void HandleFinishedConfiguring()
        {
            if (MatchLogicNetworkAdapter.Instance?.ActiveRoundManager != null)
                MatchLogicNetworkAdapter.Instance.ActiveRoundManager.OnMatchStateChange += HandleOnMatchStateChange;
        }

        protected override void OnDestroy()
        {
            if (MatchLogicNetworkAdapter.Instance != null)
                MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring -= HandleFinishedConfiguring;

            if (MatchLogicNetworkAdapter.Instance?.ActiveRoundManager != null)
                MatchLogicNetworkAdapter.Instance.ActiveRoundManager.OnMatchStateChange -= HandleOnMatchStateChange;
        }

        private void HandleOnMatchStateChange(BaseMatchState first, BaseMatchState second)
        {
            ApplySkin(skinIndex.value);
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            skinIndex.onChanged += OnSkinChanged;
            ApplySkin(skinIndex.value);
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            skinIndex.onChanged -= OnSkinChanged;
        }

        private void OnSkinChanged(int newIndex)
        {
            ApplySkin(newIndex);
        }

        private async void ApplySkin(int index)
        {
            var shouldRenderArmsOnly = await ShouldRenderArmsOnlyBasedOnAuthoritativeMatchState();

            Debug.Log($"[SkinRenderer] ApplySkin called. _tpHidden: {_tpHidden}, ShouldRenderArmsOnlyBasedOnAuthoritativeMatchState: {shouldRenderArmsOnly}");
            if (skinCatalog == null || skinCatalog.Count == 0)
                return;

            var skinData = skinCatalog.Get(index);
            if (skinData == null || skinData.bodyMeshPrefab == null)
                return;

            if (CurrentMeshInstance != null)
            {
                CurrentMeshInstance.transform.SetParent(null);
                Destroy(CurrentMeshInstance);
            }

            foreach (var kvp in _fpArmsInstances)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            _fpArmsInstances.Clear();

            if (_skillArmsInstance != null)
            {
                Destroy(_skillArmsInstance);
                _skillArmsInstance = null;
            }

            if (_grappleArmsInstance != null)
            {
                Destroy(_grappleArmsInstance);
                _grappleArmsInstance = null;
            }

            CurrentlyLoadedSkinData = skinData;

            if (shouldRenderArmsOnly)
            {
                SpawnFPArmsVariants(skinData);
                ApplyMeshPrefabAndAvatar(skinData.bodyMeshPrefab, skinData.bodyAvatar);
            }
            else
            {
                ApplyMeshPrefabAndAvatar(skinData.bodyMeshPrefab, skinData.bodyAvatar);
            }

            animator.Rebind();
            OnNewSkinSpawned.Invoke(CurrentMeshInstance);

            if (shouldRenderArmsOnly && !_tpHidden)
            {
                HideTPBody();
            }

            if (_tpHidden)
            {
                HideTPBody();
                GetComponent<FPArmsManager>()?.RefreshArms();
            }
        }

        private void SpawnFPArmsVariants(SkinData skinData)
        {
            if (fpArmsRoot == null || skinData.fpArmsVariants == null)
                return;

            foreach (var entry in skinData.fpArmsVariants)
            {
                if (entry.armsPrefab == null) continue;

                GameObject instance = Instantiate(entry.armsPrefab, fpArmsRoot);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                Animator armsAnimator = instance.GetComponent<Animator>();
                if (armsAnimator != null && entry.animatorController != null)
                {
                    armsAnimator.runtimeAnimatorController = entry.animatorController;
                }

                instance.SetActive(false);
                _fpArmsInstances[entry.weaponClass] = instance;
            }

            if (skinData.skillArmsPrefab != null)
            {
                _skillArmsInstance = Instantiate(skinData.skillArmsPrefab, fpArmsRoot);
                _skillArmsInstance.transform.localPosition = Vector3.zero;
                _skillArmsInstance.transform.localRotation = Quaternion.identity;

                Animator skillAnimator = _skillArmsInstance.GetComponent<Animator>();
                if (skillAnimator != null && skinData.skillArmsAnimatorController != null)
                {
                    skillAnimator.runtimeAnimatorController = skinData.skillArmsAnimatorController;
                }

                _skillArmsInstance.SetActive(false);
            }

            if (skinData.grappleArmsPrefab != null)
            {
                _grappleArmsInstance = Instantiate(skinData.grappleArmsPrefab, fpArmsRoot);
                _grappleArmsInstance.transform.localPosition = Vector3.zero;
                _grappleArmsInstance.transform.localRotation = Quaternion.identity;

                Animator grappleAnimator = _grappleArmsInstance.GetComponent<Animator>();
                if (grappleAnimator != null && skinData.grappleArmsAnimatorController != null)
                {
                    grappleAnimator.runtimeAnimatorController = skinData.grappleArmsAnimatorController;
                }

                _grappleArmsInstance.SetActive(false);
            }
        }

        private void ApplyMeshPrefabAndAvatar(GameObject meshPrefab, Avatar avatar)
        {
            CurrentMeshInstance = Instantiate(meshPrefab, transform);

            var innerAnimator = CurrentMeshInstance.GetComponent<Animator>();
            Destroy(innerAnimator);

            if (avatar != null)
                animator.avatar = avatar;
        }

        [ContextMenu("Try request skin")]
        public void TryRequestSkin() => RequestSkin(testSkinIndexToRequest);

        public void RequestSkin(int index) => SetSkinServerRpc(index);

        [ServerRpc]
        private void SetSkinServerRpc(int index)
        {
            if (index >= 0 && index < skinCatalog.Count)
                skinIndex.value = index;
        }

        public void HideTPBody()
        {
            _tpHidden = true;
            if (CurrentMeshInstance == null) return;

            foreach (var smr in CurrentMeshInstance.GetComponentsInChildren<SkinnedMeshRenderer>())
                smr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }
}
