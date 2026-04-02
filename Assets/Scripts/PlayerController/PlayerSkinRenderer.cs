    using System;
    using System.Collections.Generic;
    using PurrNet;
    using Resonance.Assemblies.SharedGameLogic;
    using Resonance.Combat.Weapons.Enums;
    using Resonance.Match;
    using UnityEngine;

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

            private Dictionary<WeaponClass, GameObject> _fpArmsInstances = new Dictionary<WeaponClass, GameObject>();
            public IReadOnlyDictionary<WeaponClass, GameObject> FPArmsInstances => _fpArmsInstances;

            private BaseRoundManagerNetworkAdapter roundManager;
            
            private bool _tpHidden;

            public bool ShouldRenderArmsOnly
            {
                get
                {
                    if (roundManager != null && roundManager.IsMatchActive)
                        return isOwner;
                    else if (roundManager == null)
                        return isOwner;
                    return false;
                }
            }

            private void Awake()
            {
                roundManager = MatchLogicNetworkAdapter.Instance?.ActiveRoundManager;
                roundManager.OnMatchStateChange += HandleOnMatchStateChange;
            }

            protected override void OnDestroy()
            {
                roundManager.OnMatchStateChange -= HandleOnMatchStateChange;
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

            private void ApplySkin(int index)
            {
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
                    if (kvp.Value != null) Destroy(kvp.Value);
                _fpArmsInstances.Clear();

                CurrentlyLoadedSkinData = skinData;

                if (ShouldRenderArmsOnly)
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
                        armsAnimator.runtimeAnimatorController = entry.animatorController;

                    instance.SetActive(false);
                    _fpArmsInstances[entry.weaponClass] = instance;
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
                    smr.enabled = false;

                var equipPoints = CurrentMeshInstance.transform.Find("EquipPoints");
                if (equipPoints != null)
                {
                    foreach (var mr in equipPoints.GetComponentsInChildren<MeshRenderer>())
                        mr.enabled = false;
                    foreach (var smr in equipPoints.GetComponentsInChildren<SkinnedMeshRenderer>())
                        smr.enabled = false;
                }
            }
        }
    }