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

        // The FP-arms instance currently shown (SetActive true). Single source of truth for "which
        // arms are visible", decoupled from the predicted/verified weapon class.
        private WeaponClass _shownClass;
        private bool _hasShownClass;

        private void Awake()
        {
            _skinRenderer = GetComponent<PlayerSkinRenderer>();
            _playerState = GetComponent<PlayerState>();
            _playerState.OnWeaponClassChanged += OnWeaponClassChanged;
        }

        private void Start()
        {
            // PredictedEvent exists only after PlayerSkinRenderer's LateAwake, which runs
            // before Start — so subscribe here, not in Awake.
            _skinRenderer.OnNewSkinSpawned.AddListener(OnSkinSpawned);
        }

        private void OnDestroy()
        {
            _playerState.OnWeaponClassChanged -= OnWeaponClassChanged;
            if (_skinRenderer != null)
                _skinRenderer.OnNewSkinSpawned?.RemoveListener(OnSkinSpawned);
        }

        private void OnSkinSpawned(GameObject _) => RefreshArmsForCurrentWeaponInState();

        private void OnWeaponClassChanged(WeaponClass weaponClass)
        {
            _currentWeaponClass = weaponClass;
            if (_suppressRefresh)
            {
                _suppressRefresh = false;
                return;
            }
            RefreshArmsForCurrentWeaponInState();
        }

        public void RefreshArmsForCurrentWeaponInState()
        {
            ShowClass(_currentWeaponClass);
        }

        /// <summary>
        /// Activates only the FP-arms instance for the given weapon class (bucketed) and deactivates
        /// the rest, recording it as the shown class.
        /// </summary>
        public void ShowClass(WeaponClass weaponClass)
        {
            if (_skinRenderer?.FPArmsInstances == null) return;

            WeaponClass classToShow = BucketForFpArms(weaponClass);

            Debug.Log($"[FPArmsDiag] f{Time.frameCount} ShowClass requested={weaponClass} show={classToShow}");

            foreach (var kvp in _skinRenderer.FPArmsInstances)
            {
                if (kvp.Value != null)
                    kvp.Value.SetActive(kvp.Key == classToShow);
            }

            _shownClass = classToShow;
            _hasShownClass = true;
        }

        /// <summary>
        /// The Animator on the currently-shown FP-arms instance, or null if none is shown yet.
        /// </summary>
        public Animator GetShownAnimator()
        {
            if (!_hasShownClass || _skinRenderer?.FPArmsInstances == null) return null;
            if (_skinRenderer.FPArmsInstances.TryGetValue(_shownClass, out GameObject instance))
                return instance != null ? instance.GetComponent<Animator>() : null;
            return null;
        }

        private static WeaponClass BucketForFpArms(WeaponClass weaponClass)
        {
            if (weaponClass == WeaponClass.Pistol || weaponClass == WeaponClass.Sword)
                return weaponClass;
            return WeaponClass.Rifle;
        }
        
        public WeaponView GetActiveFPWeaponView()
        {
            if (!_hasShownClass || _skinRenderer?.FPArmsInstances == null) return null;

            if (_skinRenderer.FPArmsInstances.TryGetValue(_shownClass, out GameObject arms) && arms != null)
                return arms.GetComponentInChildren<WeaponView>(true);

            return null;
        }
        
        public void SuppressNextRefresh()
        {
            _suppressRefresh = true;
        }
    }
}