using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using Resonance.Helper;
using Resonance.Inventory;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat
{
    public class PlayerEquip : MonoBehaviour
    {
        public WeaponProperties EquippedWeapon { get; private set; }
        
        private ObservableValue<WeaponProperties> equippedWeaponObservable = new ObservableValue<WeaponProperties>();
        public ObservableValue<WeaponProperties> EquippedWeaponObservable => equippedWeaponObservable;
        
        [SerializeField] PlayerInventory playerInventory;
        public PlayerInventory PlayerInventory => playerInventory;
        
        [SerializeField] Transform equipSlot;
        public Transform EquipSlot => equipSlot;
        
        [SerializeField] private PlayerActionsInput playerActionsInput;

        [SerializeField] private WeaponView currentWeaponView;
        public WeaponView CurrentWeaponView => currentWeaponView;
        

        private GameObject currentWeaponInstance;
        private PlayerStats playerStats;
        
        void Start()
        {
            playerStats = GetComponent<PlayerStats>();
            
            StartCoroutine(EquipStartingWeaponNextFrame());
        }
        
        private WeaponProperties previousWeapon;

        System.Collections.IEnumerator EquipStartingWeaponNextFrame()
        {
            yield return null;

            if (playerInventory == null)
            {
                yield break;
            }

            if (playerInventory.weaponInventory == null || playerInventory.weaponInventory.Length <= 1)
            {
                yield break;
            }

            WeaponProperties startWeapon = playerInventory.weaponInventory[1];
            if (startWeapon != null)
            {
                Equip(startWeapon);
            }
        }

        
        void Update()
        {
            if (playerActionsInput == null || playerInventory == null)
            {
                return;
            }

            if (playerActionsInput.SwapWeaponPressed)
            {
                SwapWeapon();
                playerActionsInput.SetSwapWeaponPressedFalse();
            }

            if (playerActionsInput.SwapSlotOnePressed)
            {
                EquipFromSlot(0);
                playerActionsInput.SetSlotOnePressedFalse();
            }

            if (playerActionsInput.SwapSlotTwoPressed)
            {
                EquipFromSlot(1);
                playerActionsInput.SetSlotTwoPressedFalse();
            }
        }
        
        void SwapWeapon()
        {
            if (EquippedWeapon == null)
            {
                EquipFromSlot(1);
                return;
            }

            if (EquippedWeapon.Slot == WeaponSlot.Primary)
            {
                EquipFromSlot(1);
            }
            else
            {
                EquipFromSlot(0);
            }
        }

        void EquipFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= playerInventory.weaponInventory.Length)
            {
                return;
            }

            WeaponProperties weapon = playerInventory.weaponInventory[slotIndex];
            if (weapon == null)
            {
                return;
            }

            Equip(weapon);
        }

        void Equip(WeaponProperties weapon)
        {
            Debug.Log($"Equip called with: {weapon?.name ?? "null"}");
            
            if (weapon == null)
            {
                return;
            }
            
            Debug.Log($"EquippedWeapon is currently: {EquippedWeapon?.name ?? "null"}");
            
            if (EquippedWeapon == weapon)
            {
                return;
            }

            if (EquippedWeapon != null && playerStats != null)
            {
                playerStats.RemoveSpeedModifier(EquippedWeapon.Mobility);
            }

            EquippedWeapon = weapon;

            if (equippedWeaponObservable != null)
            {
                equippedWeaponObservable.Value = weapon;
            }

            if (playerStats != null)
            {
                playerStats.AddSpeedModifier(weapon.Mobility);
            }
            
            Debug.Log("About to call RefreshWeaponView");
            RefreshWeaponView(weapon);
        }
        
        void RefreshWeaponView(WeaponProperties weapon)
        {
            
            if (currentWeaponInstance != null)
            {
                Destroy(currentWeaponInstance);
                currentWeaponInstance = null;
                currentWeaponView = null;
            }

            if (equipSlot == null)
            {
                Debug.LogError("PlayerEquip has no equipSlot assigned.", this);
                return;
            }

            if (weapon.WeaponPrefab == null)
            {
                Debug.LogError("WeaponProperties has no WeaponPrefab assigned.", weapon);
                return;
            }

            currentWeaponInstance = Instantiate(weapon.WeaponPrefab, equipSlot);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;
            currentWeaponInstance.transform.localScale = Vector3.one;

            currentWeaponView = currentWeaponInstance.GetComponent<WeaponView>();
            if (currentWeaponView == null)
            {
                Debug.LogError("WeaponPrefab is missing WeaponView component.", currentWeaponInstance);
            }
        }
    }
}
