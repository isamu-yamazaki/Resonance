using Resonance.Combat.Mods;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class AbilityHumanTurret : MonoBehaviour, IAugmentAbility, IEquippableAbility
    {
        [SerializeField] private float timeToActivate = 2f;

        [SerializeField] private float damageReduction = 0.25f;
        [SerializeField] private WeaponModProperties turretMod;

        private PlayerLocomotionInput playerLocomotionInput;
        private PlayerStats playerStats;
        private WeaponStatManager weaponStatManager;
        private PlayerShooter playerShooter;

        private float timeStandingStill;
        private bool isTurretActive;

        // This component stays permanently enabled; "equipped" is tracked here instead of via the
        // Unity `enabled` flag (see IEquippableAbility / PlayerAbilityManager).
        private bool _isEquipped;

        public string AbilityKey => "ability_humanTurret";
        public string Name => "Human Turret";
        public string Description => "Standing still long enough turns you into a turret.";
        public float MaxCooldown => timeToActivate;
        public float CurrentCooldown
        {
            get => timeStandingStill;
            set => timeStandingStill = Mathf.Clamp(value, 0f, timeToActivate);
        }
        public bool AbilityReady => false;

        public void ActivateAbilityExternal() { }
        public void SimulateActivateAbility()
        {
            throw new System.NotImplementedException();
        }

        public void SetEquipped(bool equipped)
        {
            if (_isEquipped == equipped) return;

            _isEquipped = equipped;

            // Unequipping must tear down the turret effect; OnDisable used to do this, but the
            // component no longer gets disabled.
            if (!equipped)
                DeactivateTurret();
        }

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
            playerLocomotionInput = PlayerLocomotionInput.Instance;
            weaponStatManager = GetComponent<WeaponStatManager>();
            playerShooter = GetComponent<PlayerShooter>();
            timeStandingStill = 0f;
            isTurretActive = false;
        }

        private void Update()
        {
            if (!_isEquipped) return;

            if (playerLocomotionInput.MovementInput == Vector2.zero)
            {
                StandingStill();
            }
            else
            {
                Moving();
            }
        }

        private void OnDisable()
        {
            DeactivateTurret();
        }

        private void StandingStill()
        {
            if (isTurretActive)
            {
                return;
            }

            timeStandingStill += Time.deltaTime;

            if (timeStandingStill >= timeToActivate)
            {
                ActivateTurret();
            }
        }

        private void Moving()
        {
            timeStandingStill = 0f;
            DeactivateTurret();
        }

        private void ActivateTurret()
        {
            if (isTurretActive)
            {
                return;
            }

            isTurretActive = true;

            playerStats.AddDamageReductionModifier(damageReduction);
            weaponStatManager.AddAugmentMod(turretMod);
            playerShooter.RefreshWeaponStats();
        }

        private void DeactivateTurret()
        {
            if (!isTurretActive)
            {
                return;
            }

            isTurretActive = false;

            playerStats.RemoveDamageReductionModifier(damageReduction);
            weaponStatManager.RemoveAugmentMod(turretMod);
            playerShooter.RefreshWeaponStats();
        }
    }
}
