using PurrNet;
using Resonance.Combat.Weapons;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class PlayerCamoRenderer : NetworkBehaviour
    {
        [SerializeField] private Material camoMaterial;

        private PlayerSkinRenderer playerSkinRenderer;
        private PlayerEquip playerEquip;
        private Renderer[] camoRenderers;
        private Renderer[] weaponRenderers;
        private Material[][] originalSkinMaterials;
        private Material[][] originalWeaponMaterials;
        private bool isCamoMaterialApplied;

        private SyncVar<bool> camoMaterialActive = new SyncVar<bool>(default, 0f, ownerAuth: true);
        public SyncVar<bool> CamoMaterialActive => camoMaterialActive;

        private void Awake()
        {
            playerSkinRenderer = GetComponent<PlayerSkinRenderer>();
            playerSkinRenderer.OnNewSkinSpawned += OnSkinSpawned;

            playerEquip = GetComponent<PlayerEquip>();
            playerEquip.OnWeaponInstanceReady += OnWeaponInstanceReady;
        }

        private void OnDestroy()
        {
            if (playerSkinRenderer != null)
            {
                playerSkinRenderer.OnNewSkinSpawned -= OnSkinSpawned;
            }

            if (playerEquip != null)
            {
                playerEquip.OnWeaponInstanceReady -= OnWeaponInstanceReady;
            }
        }

        private void OnSkinSpawned(GameObject skinInstance)
        {
            camoRenderers = skinInstance.GetComponentsInChildren<Renderer>();
            CacheOriginalMaterials(camoRenderers, ref originalSkinMaterials);

            if (isCamoMaterialApplied)
            {
                SwapToCamoMaterials(camoRenderers);
            }
        }

        private void OnWeaponInstanceReady(WeaponView weaponView)
        {
            if (weaponView != null)
            {
                weaponRenderers = weaponView.GetComponentsInChildren<Renderer>();
                CacheOriginalMaterials(weaponRenderers, ref originalWeaponMaterials);

                if (isCamoMaterialApplied)
                {
                    SwapToCamoMaterials(weaponRenderers);
                }
            }
            else
            {
                weaponRenderers = null;
                originalWeaponMaterials = null;
            }
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            camoMaterialActive.onChanged += OnCamoMaterialActiveChanged;

            if (playerSkinRenderer.CurrentMeshInstance != null)
            {
                camoRenderers = playerSkinRenderer.CurrentMeshInstance.GetComponentsInChildren<Renderer>();
                CacheOriginalMaterials(camoRenderers, ref originalSkinMaterials);
            }
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            camoMaterialActive.onChanged -= OnCamoMaterialActiveChanged;
        }

        private void CacheOriginalMaterials(Renderer[] renderers, ref Material[][] cache)
        {
            if (renderers == null)
            {
                return;
            }

            cache = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    cache[i] = renderers[i].materials;
                }
            }
        }

        private void OnCamoMaterialActiveChanged(bool active)
        {
            isCamoMaterialApplied = active;

            if (active)
            {
                SwapToCamoMaterials(camoRenderers);
                SwapToCamoMaterials(weaponRenderers);
            }
            else
            {
                RestoreOriginalMaterials(camoRenderers, originalSkinMaterials);
                RestoreOriginalMaterials(weaponRenderers, originalWeaponMaterials);
            }
        }

        private void SwapToCamoMaterials(Renderer[] renderers)
        {
            if (renderers == null || camoMaterial == null)
            {
                return;
            }

            foreach (Renderer r in renderers)
            {
                if (r == null)
                {
                    continue;
                }

                Material[] newMats = new Material[r.materials.Length];
                for (int i = 0; i < newMats.Length; i++)
                {
                    newMats[i] = camoMaterial;
                }

                r.materials = newMats;
            }
        }

        private void RestoreOriginalMaterials(Renderer[] renderers, Material[][] cache)
        {
            if (renderers == null || cache == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && cache[i] != null)
                {
                    renderers[i].materials = cache[i];
                }
            }
        }

        private void OnDisable()
        {
            RestoreOriginalMaterials(camoRenderers, originalSkinMaterials);
            RestoreOriginalMaterials(weaponRenderers, originalWeaponMaterials);
            isCamoMaterialApplied = false;
        }
    }
}