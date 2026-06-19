using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet.Prediction;
using Resonance.Assemblies.SharedGameLogic;
using Resonance.Combat.Weapons.Enums;
using Resonance.LobbySystem.DataProviders;
using Resonance.Match;
using UnityEngine;
using UnityEngine.Rendering;

namespace Resonance.PlayerController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerSkinRenderer : PredictedIdentity<PlayerSkinRendererInputData, PlayerSkinRendererDataState>
    {
        [SerializeField] private SkinCatalog skinCatalog;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform fpArmsRoot;
        public Action<GameObject> OnNewSkinSpawned;

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

        private PlayerSkinRendererDataState? _previousStateFromServer;

        public bool ShouldRenderArmsOnlyBasedOnCachedMatchState
        {
            get
            {
                var roundManager = MatchLogicNetworkAdapter.Instance?.GetTemporaryActiveRoundManagerReference();
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
            var roundManager = MatchLogicNetworkAdapter.Instance?.GetTemporaryActiveRoundManagerReference();
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

        protected override void OnDestroy()
        {
            if (MatchLogicNetworkAdapter.Instance != null)
                MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring -= HandleFinishedConfiguring;

            var roundManager = MatchLogicNetworkAdapter.Instance?.GetTemporaryActiveRoundManagerReference();
            if (roundManager != null)
                roundManager.OnMatchStateChange -= HandleOnMatchStateChange;
        }

        private void HandleFinishedConfiguring()
        {
            var roundManager = MatchLogicNetworkAdapter.Instance?.GetTemporaryActiveRoundManagerReference();
            if (roundManager != null)
                roundManager.OnMatchStateChange += HandleOnMatchStateChange;
        }

        private void HandleOnMatchStateChange(BaseMatchState first, BaseMatchState second)
        {
            // Match state changes switch between FP arms / TP model without changing the
            // skin index, so bypass the dedup guard and reapply directly.
            ApplySkin(currentState.SkinIndex);
        }

        #region PredictedIdentity overrides

        protected override PlayerSkinRendererDataState GetInitialState()
        {
            return new PlayerSkinRendererDataState { SkinIndex = 0 };
        }

        protected override void GetFinalInput(ref PlayerSkinRendererInputData input)
        {
            if (!isOwner || SkinIndexProvider.Instance == null) return;
            input.HasSkinRequest = true;
            input.SkinIndex = SkinIndexProvider.Instance.SkinIndex;
        }

        protected override void Simulate(PlayerSkinRendererInputData input, ref PlayerSkinRendererDataState state, float delta)
        {
            if (input.HasSkinRequest && skinCatalog != null
                && input.SkinIndex >= 0 && input.SkinIndex < skinCatalog.Count)
            {
                state.SkinIndex = input.SkinIndex;
            }

            if (predictionManager.isVerified)
            {
                if (_previousStateFromServer?.SkinIndex != state.SkinIndex)
                {
                    _ = ApplySkin(state.SkinIndex);
                }

                _previousStateFromServer = state;
            }
        }

        protected override PlayerSkinRendererDataState Interpolate(
            PlayerSkinRendererDataState from,
            PlayerSkinRendererDataState to,
            float t)
        {
            return to;
        }

        protected override void UpdateView(PlayerSkinRendererDataState viewState, PlayerSkinRendererDataState? verified)
        {
        }

        #endregion

        #region Skin application

        [ContextMenu("Try request skin")]
        public void TryRequestSkin() => SkinIndexProvider.Instance?.SetSkinIndex(testSkinIndexToRequest);

        private async Task ApplySkin(int index)
        {
            var shouldRenderArmsOnly = await ShouldRenderArmsOnlyBasedOnAuthoritativeMatchState();

#if UNITY_EDITOR
            Debug.Log($"[SkinRenderer] ApplySkin called. _tpHidden: {_tpHidden}, ShouldRenderArmsOnlyBasedOnAuthoritativeMatchState: {shouldRenderArmsOnly}");
#endif
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

        public void HideTPBody()
        {
            _tpHidden = true;
            if (CurrentMeshInstance == null) return;

            foreach (var smr in CurrentMeshInstance.GetComponentsInChildren<SkinnedMeshRenderer>())
                smr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        #endregion
    }
}
