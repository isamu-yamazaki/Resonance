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
    [RequireComponent(typeof(PredictedTransform))]
    [DefaultExecutionOrder(-2)]
    public class PlayerSkinRenderer : PredictedIdentity<PlayerSkinRendererInputData, PlayerSkinRendererDataState>
    {
        [SerializeField] private SkinCatalog skinCatalog;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform fpArmsRoot;

        [SerializeField] private int testSkinIndexToRequest = 0;

        private PredictedTransform _predictedTransform;

        public PredictedEvent<GameObject> OnNewSkinSpawned { get; private set; }

        /// <summary>
        /// Current mesh instance based on the simulated verified tick.
        /// </summary>
        public GameObject CurrentMeshInstance { get; private set; }

        /// <summary>
        /// Currently loaded skin data based on the simulated verified tick.
        /// </summary>
        public SkinData CurrentlyLoadedSkinData { get; private set; }

        private GameObject _skillArmsInstance;
        public GameObject SkillArmsInstance => _skillArmsInstance;

        private GameObject _grappleArmsInstance;
        public GameObject GrappleArmsInstance => _grappleArmsInstance;

        private Dictionary<WeaponClass, GameObject> _fpArmsInstances = new Dictionary<WeaponClass, GameObject>();
        public IReadOnlyDictionary<WeaponClass, GameObject> FPArmsInstances => _fpArmsInstances;

        private bool _pendingSkinRequest;
        private bool _hasRenderedArmsOnce;

        public bool ShouldRenderArmsOnlyBasedOnCachedMatchState
        {
            get
            {
                var roundManager = MatchLogicNetworkAdapter.Instance?.GetTemporaryActiveRoundManagerReference();
                if (roundManager != null)
                {
                    return roundManager.IsMatchActive && isOwner;
                }

                return isOwner;
            }
        }

        private async Task<bool> ShouldRenderArmsOnlyBasedOnAuthoritativeMatchState()
        {
            var roundManager = MatchLogicNetworkAdapter.Instance?.GetTemporaryActiveRoundManagerReference();
            if (roundManager != null)
            {
                var matchState = await roundManager.GetMatchState();

                return (BaseMatchState)matchState == BaseMatchState.MatchActive && isOwner;
            }

            return isOwner;
        }

        #region Lifecycle

        protected override PlayerSkinRendererDataState GetInitialState()
        {
            return new PlayerSkinRendererDataState { SkinIndex = 0, LastSkinIndex = -1 };
        }

        protected override void LateAwake()
        {
            // Build once and reuse across pool reuse so listeners that subscribed in their
            // own Start() survive. LateAwake runs after predictionManager is assigned and
            // before any Simulate (and thus before the first Invoke).
            OnNewSkinSpawned ??= new PredictedEvent<GameObject>(predictionManager, this);
            _predictedTransform = GetComponent<PredictedTransform>();
        }

        #endregion

        #region Input

        protected override void GetFinalInput(ref PlayerSkinRendererInputData input)
        {
            if (!isOwner || SkinIndexProvider.Instance == null) return;
            input.HasSkinRequest = true;
            input.SkinIndex = SkinIndexProvider.Instance.SkinIndex;
        }

        #endregion

        #region Simulation

        protected override void Simulate(PlayerSkinRendererInputData input, ref PlayerSkinRendererDataState state,
            float delta)
        {
            if (input.HasSkinRequest && skinCatalog != null
                                     && input.SkinIndex >= 0 && input.SkinIndex < skinCatalog.Count)
            {
                state.SkinIndex = input.SkinIndex;
            }

            if (predictionManager.isVerified)
            {
                SimulateVerifiedTick(ref state);
            }
        }

        /// <summary>
        /// Run client-side and server-side logic, including side effects like
        /// actually applying a skin, behind a verified tick.
        /// </summary>
        /// <param name="state"></param>
        private void SimulateVerifiedTick(ref PlayerSkinRendererDataState state)
        {
            if (CurrentMeshInstance == null || state.LastSkinIndex != state.SkinIndex)
            {
                ApplySkinAsSideEffectOfVerifiedTick(state.SkinIndex);
            }

            state.LastSkinIndex = state.SkinIndex;
        }

        [SimulationOnly]
        private void ApplySkinAsSideEffectOfVerifiedTick(int index)
        {
#if UNITY_EDITOR
            Debug.Log("[PlayerSkinRenderer] ApplySkinAsSideEffectOfVerifiedTick called");
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

            ApplyMeshPrefabAndAvatar(skinData.bodyMeshPrefab, skinData.bodyAvatar);

            animator.Rebind();
            OnNewSkinSpawned?.Invoke(CurrentMeshInstance);
        }


        private void ApplyMeshPrefabAndAvatar(GameObject meshPrefab, Avatar avatar)
        {
            // Parent the third-person body under the interpolated graphics root (owned by the
            // sibling PredictedTransform, exposed via the movement controller) so it follows the
            // smooth transform rather than the raw simulated root. Falls back to this transform.
            var bodyParent = _predictedTransform.graphics ?? transform;
            CurrentMeshInstance = Instantiate(meshPrefab, bodyParent);

            var innerAnimator = CurrentMeshInstance.GetComponent<Animator>();
            Destroy(innerAnimator);

            if (avatar != null)
                animator.avatar = avatar;
        }

        #endregion

        #region Local view updates

        protected override void UpdateView(PlayerSkinRendererDataState viewState, PlayerSkinRendererDataState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            var shouldSpawnArmsBasedOnMatchState = ShouldRenderArmsOnlyBasedOnCachedMatchState && isOwner;
            var skinData = skinCatalog.Get(v.SkinIndex);

            if ((!shouldSpawnArmsBasedOnMatchState || _hasRenderedArmsOnce) && v.LastSkinIndex == v.SkinIndex) return;

            if (skinData != null)
            {
                SpawnFpArmsVariants(skinData);
            }

            HideTPBody();
            GetComponent<FPArmsManager>()?.RefreshArmsForCurrentWeaponInState();

            _hasRenderedArmsOnce = true;
        }

        public void HideTPBody()
        {
            if (CurrentMeshInstance == null) return;

            foreach (var smr in CurrentMeshInstance.GetComponentsInChildren<SkinnedMeshRenderer>())
                smr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        private void SpawnFpArmsVariants(SkinData skinData)
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

        protected override PlayerSkinRendererDataState Interpolate(
            PlayerSkinRendererDataState from,
            PlayerSkinRendererDataState to,
            float t)
        {
            return to;
        }

        #endregion

        #region Debugging

        [ContextMenu("Try request skin")]
        public void TryRequestSkin() => SkinIndexProvider.Instance?.SetSkinIndex(testSkinIndexToRequest);

        #endregion
    }
}