using System;
using PurrNet;
using Resonance.Assemblies.SharedGameLogic;
using Resonance.Match;
using UnityEngine;

namespace Resonance.PlayerController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerSkinRenderer : NetworkBehaviour
    {
        [SerializeField] private SkinCatalog skinCatalog;
        [SerializeField] private Animator animator;
        public Action<GameObject> OnNewSkinSpawned;

        public SyncVar<int> skinIndex = new SyncVar<int>();

        [SerializeField] private int testSkinIndexToRequest = 0;

        public GameObject CurrentMeshInstance { get; private set; }
        public SkinData CurrentlyLoadedSkinData { get; private set; }

        private BaseRoundManagerNetworkAdapter roundManager;

        public bool ShouldRenderArmsOnly
        {
            get
            {
                if (roundManager != null && roundManager.IsMatchActive)
                {
                    return isOwner;
                }
                else if (roundManager == null)
                {
                    return isOwner;
                }
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
            {
                return;
            }

            var skinData = skinCatalog.Get(index);
            if (skinData == null || skinData.bodyMeshPrefab == null)
            {
                return;
            }

            if (CurrentMeshInstance != null)
            {
                Destroy(CurrentMeshInstance);
            }

            CurrentlyLoadedSkinData = skinData;

            if (ShouldRenderArmsOnly)
            {
                ApplyMeshPrefabAndAvatar(skinData.armsMeshPrefab, skinData.armsAvatar);
            }
            else
            {
                ApplyMeshPrefabAndAvatar(skinData.bodyMeshPrefab, skinData.bodyAvatar);
            }

            animator.Rebind();

            OnNewSkinSpawned.Invoke(CurrentMeshInstance);
        }

        private void ApplyMeshPrefabAndAvatar(GameObject meshPrefab, Avatar avatar)
        {
            CurrentMeshInstance = Instantiate(meshPrefab, transform);

            var innerAnimator = CurrentMeshInstance.GetComponent<Animator>();
            Destroy(innerAnimator);

            if (avatar != null)
            {
                animator.avatar = avatar;
            }
        }

        [ContextMenu("Try request skin")]
        public void TryRequestSkin()
        {
            RequestSkin(testSkinIndexToRequest);
        }

        public void RequestSkin(int index)
        {
            SetSkinServerRpc(index);
        }

        [ServerRpc]
        private void SetSkinServerRpc(int index)
        {
            if (index >= 0 && index < skinCatalog.Count)
            {
                skinIndex.value = index;
            }
        }
    }
}
