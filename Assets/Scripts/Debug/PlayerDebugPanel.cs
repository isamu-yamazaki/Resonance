using UnityEngine;
using Resonance.PlayerController;
using Resonance.Player;

namespace Resonance.DebugTools
{
    public class PlayerDebugPanel : MonoBehaviour
    {
        #region Class Variables
        private PlayerController.PlayerController _playerController;
        private PlayerState _playerState;
        private PlayerLocomotionInput _playerInput;
        private OverdriveAbility _overdriveAbility;
        private CharacterController _characterController;
        private PlayerStats _playerStats;
        private Vector2 _scrollPosition = Vector2.zero;

        // Health inputs
        private string _damageAmount = "10";
        private string _healAmount = "25";

        // Modifier inputs
        private string _speedModifierAmount = "1";
        private string _drModifierAmount = "1";
        private string _regenModifierAmount = "0";

        // Active debug modifiers (so we can remove them)
        private float? _activeSpeedModifier = null;
        private float? _activeDRModifier = null;
        private float? _activeRegenModifier = null;
        #endregion

        #region Startup
        private void Start()
        {
            FindPlayerComponents();
        }

        private void FindPlayerComponents()
        {
            var players = GameObject.FindGameObjectsWithTag("Player");
            GameObject player = null;

            foreach (var obj in players)
            {
                if (obj.GetComponent<PlayerStats>() != null)
                {
                    player = obj;
                    break;
                }
            }

            if (player == null)
            {
                Debug.LogWarning("PlayerDebugPanel: No GameObject with PlayerStats found!");
                return;
            }

            _playerController = player.GetComponent<PlayerController.PlayerController>();
            _playerState = player.GetComponent<PlayerState>();
            _playerInput = player.GetComponent<PlayerLocomotionInput>();
            _overdriveAbility = player.GetComponent<OverdriveAbility>();
            _characterController = player.GetComponent<CharacterController>();
            _playerStats = player.GetComponent<PlayerStats>();
        }
        #endregion

        #region Public Methods
        public void DrawPanel()
        {
            if (_playerController == null)
                FindPlayerComponents();

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(300), GUILayout.Height(500));
            GUILayout.BeginVertical("box");
            GUILayout.Label("=== PLAYER DEBUG ===");
            GUILayout.Space(10);

            if (_playerController == null)
            {
                GUILayout.Label("No player found! (Need PlayerStats component)");
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                return;
            }

            // Movement Stats
            GUILayout.Label("Movement Stats:");
            GUILayout.Space(5);
            DrawMovementStats();

            GUILayout.Space(15);

            // Overdrive
            GUILayout.Label("Overdrive:");
            GUILayout.Space(5);
            DrawOverdriveStats();

            GUILayout.Space(15);

            // Health
            GUILayout.Label("Health:");
            GUILayout.Space(5);
            DrawHealthStats();

            GUILayout.Space(15);

            // Modifiers
            GUILayout.Label("Stat Modifiers:");
            GUILayout.Space(5);
            DrawSpeedModifiers();

            GUILayout.Space(10);
            DrawDamageReductionModifiers();

            GUILayout.Space(10);
            DrawRegenModifiers();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }
        #endregion

        #region Draw Methods
        private void DrawMovementStats()
        {
            GUILayout.BeginVertical("box");

            Vector3 velocity = _characterController.velocity;
            GUILayout.Label($"Speed: {velocity.magnitude:F2} m/s");
            GUILayout.Label($"Velocity: ({velocity.x:F1}, {velocity.y:F1}, {velocity.z:F1})");
            GUILayout.Label($"State: {_playerState.CurrentPlayerMovementState}");
            GUILayout.Label($"Grounded: {_playerState.InGroundedState()}");

            if (_playerStats != null)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Base Speed: {_playerStats.BaseSpeed:F2}");
                GUILayout.Label($"Effective Speed Multiplier: {_playerStats.PlayerSpeed:F2}");
            }

            if (_playerInput != null)
            {
                GUILayout.Label($"Input: ({_playerInput.MovementInput.x:F2}, {_playerInput.MovementInput.y:F2})");
            }

            GUILayout.EndVertical();
        }

        private void DrawOverdriveStats()
        {
            if (_overdriveAbility == null)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("No OverdriveAbility found");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical("box");

            string status = _overdriveAbility.CurrentState.ToString();
            Color statusColor = _overdriveAbility.IsInOverdrive ? Color.green :
                               _overdriveAbility.IsOnCooldown ? Color.red : Color.white;

            GUI.color = statusColor;
            GUILayout.Label($"Status: {status}");
            GUI.color = Color.white;

            if (_overdriveAbility.IsInOverdrive)
                GUILayout.Label($"Duration: {_overdriveAbility.DurationTimeRemaining:F1}s");
            else if (_overdriveAbility.IsOnCooldown)
                GUILayout.Label($"Cooldown: {_overdriveAbility.CooldownTimeRemaining:F1}s");
            else
                GUILayout.Label("Ready!");

            GUILayout.Space(10);

            if (GUILayout.Button("Reset Cooldown (Debug)", GUILayout.Height(30)))
                ResetOverdriveCooldown();

            GUILayout.EndVertical();
        }

        private void DrawHealthStats()
        {
            if (_playerStats == null)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("No PlayerStats found");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical("box");

            GUILayout.Label($"Current Health: {_playerStats.CurrentHealth.value:F0} / {_playerStats.MaxHealth:F0}");
            GUILayout.Label($"Health Regen: {_playerStats.CurrentHealthRegen:F1}/s (base: {_playerStats.BaseHealthRegen:F1})");
            GUILayout.Label($"Damage Reduction: {_playerStats.DamageReduction:F1}%");

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Damage Amount:", GUILayout.Width(120));
            _damageAmount = GUILayout.TextField(_damageAmount, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Damage", GUILayout.Height(30), GUILayout.Width(200)))
                ApplyDamage();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Heal Amount:", GUILayout.Width(120));
            _healAmount = GUILayout.TextField(_healAmount, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            if (GUILayout.Button("Apply Heal", GUILayout.Height(30)))
                ApplyHeal();

            GUILayout.Space(5);
            if (GUILayout.Button("Heal to Max", GUILayout.Height(25)))
                HealToMax();

            GUILayout.EndVertical();
        }

private void DrawSpeedModifiers()
{
    GUILayout.BeginVertical("box");
    GUILayout.Label("Speed Modifier:");

    GUILayout.BeginHorizontal();
    GUILayout.Label("Multiplier:", GUILayout.Width(100));
    _speedModifierAmount = GUILayout.TextField(_speedModifierAmount, GUILayout.Width(60));
    GUILayout.EndHorizontal();

    GUILayout.Space(5);
    GUILayout.Label($"Effective Speed: {_playerStats.PlayerSpeed:F2}");

    // Show raw modifier list
    var speedMods = _playerStats.SpeedModifiers;
    if (speedMods.Count == 0)
    {
        GUILayout.Label("  Modifiers: (none)");
    }
    else
    {
        for (int i = 0; i < speedMods.Count; i++)
            GUILayout.Label($"  [{i}] {speedMods[i]:F2}x");
    }

    GUILayout.Space(5);

    if (_activeSpeedModifier.HasValue)
    {
        GUI.color = Color.yellow;
        GUILayout.Label($"Debug modifier active: {_activeSpeedModifier.Value:F2}x");
        GUI.color = Color.white;
        if (GUILayout.Button("Remove Speed Modifier", GUILayout.Height(25)))
            RemoveSpeedModifier();
    }
    else
    {
        if (GUILayout.Button("Add Speed Modifier", GUILayout.Height(25)))
            AddSpeedModifier();
    }

    GUILayout.EndVertical();
}

private void DrawDamageReductionModifiers()
{
    GUILayout.BeginVertical("box");
    GUILayout.Label("Damage Reduction Modifier:");

    GUILayout.BeginHorizontal();
    GUILayout.Label("DR Amount:", GUILayout.Width(100));
    _drModifierAmount = GUILayout.TextField(_drModifierAmount, GUILayout.Width(60));
    GUILayout.EndHorizontal();

    GUILayout.Space(5);
    GUILayout.Label($"Effective DR: {_playerStats.DamageReduction * 100f:F1}%");

    // Show raw modifier list
    var drMods = _playerStats.DamageReductionModifiers;
    if (drMods.Count == 0)
    {
        GUILayout.Label("  Modifiers: (none)");
    }
    else
    {
        for (int i = 0; i < drMods.Count; i++)
            GUILayout.Label($"  [{i}] {drMods[i] * 100f:F1}%");
    }

    GUILayout.Space(5);

    if (_activeDRModifier.HasValue)
    {
        GUI.color = Color.yellow;
        GUILayout.Label($"Debug modifier active: {_activeDRModifier.Value * 100f:F1}%");
        GUI.color = Color.white;
        if (GUILayout.Button("Remove DR Modifier", GUILayout.Height(25)))
            RemoveDRModifier();
    }
    else
    {
        if (GUILayout.Button("Add DR Modifier", GUILayout.Height(25)))
            AddDRModifier();
    }

    GUILayout.EndVertical();
}

private void DrawRegenModifiers()
{
    GUILayout.BeginVertical("box");
    GUILayout.Label("Regen Modifier:");

    GUILayout.BeginHorizontal();
    GUILayout.Label("Flat Amount:", GUILayout.Width(100));
    _regenModifierAmount = GUILayout.TextField(_regenModifierAmount, GUILayout.Width(60));
    GUILayout.EndHorizontal();

    GUILayout.Space(5);
    GUILayout.Label($"Effective Regen: {_playerStats.CurrentHealthRegen:F2}/s (base: {_playerStats.BaseHealthRegen:F1})");

    // Show raw modifier list
    var regenMods = _playerStats.RegenModifiers;
    if (regenMods.Count == 0)
    {
        GUILayout.Label("  Modifiers: (none)");
    }
    else
    {
        for (int i = 0; i < regenMods.Count; i++)
            GUILayout.Label($"  [{i}] +{regenMods[i]:F2}/s");
    }

    GUILayout.Space(5);

    if (_activeRegenModifier.HasValue)
    {
        GUI.color = Color.yellow;
        GUILayout.Label($"Debug modifier active: +{_activeRegenModifier.Value:F2}/s");
        GUI.color = Color.white;
        if (GUILayout.Button("Remove Regen Modifier", GUILayout.Height(25)))
            RemoveRegenModifier();
    }
    else
    {
        if (GUILayout.Button("Add Regen Modifier", GUILayout.Height(25)))
            AddRegenModifier();
    }

    GUILayout.EndVertical();
}
        #endregion

        #region Modifier Methods
        private void AddSpeedModifier()
        {
            if (_playerStats == null) return;
            if (!float.TryParse(_speedModifierAmount, out float val)) { Debug.LogWarning("Invalid speed modifier"); return; }
            _activeSpeedModifier = val;
            _playerStats.AddSpeedModifier(val);
            Debug.Log($"Added speed modifier: {val}");
        }

        private void RemoveSpeedModifier()
        {
            if (_playerStats == null || !_activeSpeedModifier.HasValue) return;
            _playerStats.RemoveSpeedModifier(_activeSpeedModifier.Value);
            Debug.Log($"Removed speed modifier: {_activeSpeedModifier.Value}");
            _activeSpeedModifier = null;
        }

        private void AddDRModifier()
        {
            if (_playerStats == null) return;
            if (!float.TryParse(_drModifierAmount, out float val)) { Debug.LogWarning("Invalid DR modifier"); return; }
            _activeDRModifier = val;
            _playerStats.AddDamageReductionModifier(val);
            Debug.Log($"Added DR modifier: {val}");
        }

        private void RemoveDRModifier()
        {
            if (_playerStats == null || !_activeDRModifier.HasValue) return;
            _playerStats.RemoveDamageReductionModifier(_activeDRModifier.Value);
            Debug.Log($"Removed DR modifier: {_activeDRModifier.Value}");
            _activeDRModifier = null;
        }

        private void AddRegenModifier()
        {
            if (_playerStats == null) return;
            if (!float.TryParse(_regenModifierAmount, out float val)) { Debug.LogWarning("Invalid regen modifier"); return; }
            _activeRegenModifier = val;
            _playerStats.AddRegenModifier(val);
            Debug.Log($"Added regen modifier: {val}");
        }

        private void RemoveRegenModifier()
        {
            if (_playerStats == null || !_activeRegenModifier.HasValue) return;
            _playerStats.RemoveRegenModifier(_activeRegenModifier.Value);
            Debug.Log($"Removed regen modifier: {_activeRegenModifier.Value}");
            _activeRegenModifier = null;
        }
        #endregion

        #region Health Methods
        private void ApplyDamage()
        {
            if (_playerStats == null) return;
            if (float.TryParse(_damageAmount, out float damage))
            {
                _playerStats.TakeDamage(damage);
                Debug.Log($"Applied {damage} damage via debug tools");
            }
            else Debug.LogWarning("Invalid damage amount");
        }

        private void ApplyHeal()
        {
            if (_playerStats == null) return;
            if (float.TryParse(_healAmount, out float healAmount))
            {
                _playerStats.Heal(healAmount);
                Debug.Log($"Applied {healAmount} heal via debug tools");
            }
            else Debug.LogWarning("Invalid heal amount");
        }

        private void HealToMax()
        {
            if (_playerStats == null) return;
            _playerStats.Heal(_playerStats.MaxHealth);
            Debug.Log("Player healed to max via debug tools");
        }
        #endregion

        #region Overdrive Reset
        private void ResetOverdriveCooldown()
        {
            if (_overdriveAbility == null) return;

            var type = _overdriveAbility.GetType();
            var overdriveStateType = type.GetNestedType("OverdriveState");
            if (overdriveStateType != null)
            {
                var readyValue = System.Enum.Parse(overdriveStateType, "Ready");
                var setStateMethod = type.GetMethod("SetState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                setStateMethod?.Invoke(_overdriveAbility, new object[] { readyValue });
            }

            var cooldownField = type.GetProperty("CooldownTimeRemaining");
            if (cooldownField != null && cooldownField.CanWrite)
                cooldownField.SetValue(_overdriveAbility, 0f);

            Debug.Log("Overdrive cooldown reset via debug tools");
        }
        #endregion
    }
}
