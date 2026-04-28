using System;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace Resonance.PlayerController
{
    public class FPArmsManager : MonoBehaviour
    {
        [SerializeField] private PlayerState _playerState;
        [SerializeField] private PlayerSkinRenderer _skinRenderer;

        private bool _suppressRefresh = false;
        private WeaponClass _currentWeaponClass;

        private void Awake()
        {
            _skinRenderer = GetComponent<PlayerSkinRenderer>();
            _playerState = GetComponent<PlayerState>();
            _playerState.OnWeaponClassChanged += OnWeaponClassChanged;
            _skinRenderer.OnNewSkinSpawned += _ => RefreshArms();
        }

        private void OnDestroy()
        {
            _playerState.OnWeaponClassChanged -= OnWeaponClassChanged;
        }

        private void OnWeaponClassChanged(WeaponClass weaponClass)
        {
            _currentWeaponClass = weaponClass;
            if (_suppressRefresh)
            {
                _suppressRefresh = false;
                return;
            }
            RefreshArms();
        }

        public void RefreshArms()
        {
            if (_skinRenderer?.FPArmsInstances == null) return;
            
            WeaponClass classToShow = _currentWeaponClass;
            if (classToShow != WeaponClass.Pistol && classToShow != WeaponClass.Sword)
                classToShow = WeaponClass.Rifle;
            
#if UNITY_EDITOR
            Debug.Log("Class to show" + classToShow.ToString());
#endif

            foreach (var kvp in _skinRenderer.FPArmsInstances)
            {
#if UNITY_EDITOR
                Debug.Log(kvp.Key);
#endif
                if (kvp.Value != null)
                    kvp.Value.SetActive(kvp.Key == classToShow);
            }
        }
        
        public WeaponView GetActiveFPWeaponView()
        {
            if (_skinRenderer?.FPArmsInstances == null) return null;

            WeaponClass classToShow = _currentWeaponClass;
            if (classToShow != WeaponClass.Pistol && classToShow != WeaponClass.Sword)
                classToShow = WeaponClass.Rifle;

            if (_skinRenderer.FPArmsInstances.TryGetValue(classToShow, out GameObject arms) && arms != null)
                return arms.GetComponentInChildren<WeaponView>(true);

            return null;
        }
        
        public void SuppressNextRefresh()
        {
            _suppressRefresh = true;
        }
    }
}