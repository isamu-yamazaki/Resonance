using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Combat;
using Resonance.PlayerController;
using UnityEngine.InputSystem;

public class HealthStimHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownFill; // gray overlay
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI keybindText;
    [SerializeField] private TextMeshProUGUI chargesText;

    [Header("Colors")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color fadedColor = new Color(1f,1f,1f,0.4f);
    
    [Header("Overlay Colors")]
    [SerializeField] private Color lightOverlayColor = new Color(0f, 0f, 0f, 0.4f);
    [SerializeField] private Color darkOverlayColor = new Color(0f, 0f, 0f, 0.75f);

    private PlayerHealthStim healthStim;
    private int currentCharges;

    private void Start()
    {
        // Wait for the local player to exist, then bind HUD
        StartCoroutine(InitializeWhenPlayerReady());
    }

    private System.Collections.IEnumerator InitializeWhenPlayerReady()
    {
        // Wait until local player exists
        while (PlayerPredictedController.LocalPlayer == null)
        {
            yield return null;
        }

        healthStim = PlayerPredictedController.LocalPlayer.GetComponent<PlayerHealthStim>();
        if (healthStim == null)
        {
            Debug.LogError("[HealthStimHUD] PlayerHealthStim not found on local player!");
            yield break;
        }

        RegisterHealthStim(healthStim);
        
        if (keybindText != null)
        {
            var controls = Resonance.PlayerController.PlayerInputManager.Instance.PlayerControls;
            keybindText.text = controls.PlayerActionMap.Stim.GetBindingDisplayString().ToUpper();
        }
    }

    private void RegisterHealthStim(PlayerHealthStim stim)
    {
        healthStim = stim;

        healthStim.CurrentCharges.ChangeEvent += OnChargesChanged;
        healthStim.ChargeCooldownRemaining.ChangeEvent += OnCooldownChanged;
        healthStim.ChargeCooldownFill.ChangeEvent += OnCooldownFillChanged;

        // Initialize UI immediately
        RefreshAll();
    }

    private void RefreshAll()
    {
        OnChargesChanged(healthStim.CurrentCharges.Value);
        OnCooldownChanged(healthStim.ChargeCooldownRemaining.Value);
        OnCooldownFillChanged(healthStim.ChargeCooldownFill.Value);
    }

    #region View Updates
    private void OnChargesChanged(int charges)
    {
        currentCharges = charges;

        chargesText.text = charges.ToString();
        icon.color = charges > 0 ? readyColor : fadedColor;
    }

    private void OnCooldownChanged(float time)
    {
        if (time > 0f)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = $"{time:F1}s";
        }
        else
        {
            timerText.gameObject.SetActive(false);
        }
    }

    private void OnCooldownFillChanged(float fill)
    {
        if (fill > 0f)
        {
            cooldownFill.gameObject.SetActive(true);
            cooldownFill.fillAmount = fill;

            cooldownFill.color = currentCharges > 0 
                ? lightOverlayColor 
                : darkOverlayColor;
        }
        else
        {
            cooldownFill.gameObject.SetActive(false);
        }
    }
    #endregion
}