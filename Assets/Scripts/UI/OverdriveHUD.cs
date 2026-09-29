using System;
using System.Collections;
using Resonance.Helper;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.PlayerController;
using UnityEngine.InputSystem;

public class OverdriveHUD : MonoBehaviour
{
    private OverdriveAbility _overdrive;

    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI keybindText;

    [Header("Colors")] [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color fadedColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color activeColor = Color.cyan;

    [Header("Text Colors")] [SerializeField]
    private Color readyTextColor = Color.white;

    [SerializeField] private Color cooldownTextColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color activeTextColor = Color.white;

    [Header("Pulse Animation")] [SerializeField]
    private float pulseSpeed = 3f;

    [SerializeField] private float pulseAmount = 0.1f;

    [Header("Active Warning")] [SerializeField]
    private float lowTimeWarningThreshold = 2f;

    [SerializeField] private Color lowTimeColor = Color.red;

    [Header("Ready Glow")] [SerializeField]
    private Outline readyGlow;

    [SerializeField] private float glowPulseSpeed = 2f;
    [SerializeField] private float glowMaxAlpha = 1f;

    private bool animateReady = false;
    private bool animateActive = false;
    private Vector3 originalScale;

    private void Awake()
    {
        if (icon != null)
            originalScale = icon.transform.localScale;

        // No player reference yet — will be set by OverdriveAbility
    }

    private void Start()
    {
        if (keybindText != null)
        {
            var controls = Resonance.PlayerController.PlayerInputManager.Instance.PlayerControls;
            keybindText.text = controls.PlayerActionMap.Overdrive.GetBindingDisplayString().ToUpper();
        }

        StartCoroutine(BindToOverdriveAbility());
    }

    private IEnumerator BindToOverdriveAbility()
    {
        while (OwnerFinder.FindFirstOwnedPredictedObjectByType<OverdriveAbility>() == null)
        {
            yield return null;
        }


        _overdrive = OwnerFinder.FindFirstOwnedPredictedObjectByType<OverdriveAbility>();
        _overdrive.OnOverdriveStateChanged += OnStateChanged;
        _overdrive.OnCooldownChanged += OnCooldownChanged;
        _overdrive.OnDurationChanged += OnDurationChanged;
        _overdrive.OnCooldownFillChanged += OnCooldownFillChanged;
    }

    private void Update()
    {
        if (animateReady)
            PulseIcon();

        if (animateReady || animateActive)
            AnimateReadyGlow();
    }

    private void OnDestroy()
    {
        if (_overdrive == null) return;
        _overdrive.OnOverdriveStateChanged -= OnStateChanged;
        _overdrive.OnCooldownChanged -= OnCooldownChanged;
        _overdrive.OnDurationChanged -= OnDurationChanged;
        _overdrive.OnCooldownFillChanged -= OnCooldownFillChanged;
    }

    #region Display States

    private void ShowReady()
    {
        icon.color = readyColor;
        cooldownFill.fillAmount = 0f;
        SetAlpha(cooldownFill, 0f);

        timerText.text = "";
        timerText.color = readyTextColor;

        animateReady = true;
        animateActive = false;

        ResetIconScale();
        EnableGlow(readyColor);
    }

    private void ShowCooldown()
    {
        float fill = _overdrive.CooldownTimeRemaining / _overdrive.CooldownDuration;
        icon.color = fadedColor;

        SetAlpha(cooldownFill, 1f);
        cooldownFill.fillAmount = fill;

        timerText.text = $"{_overdrive.CooldownTimeRemaining:F1}s";
        timerText.color = cooldownTextColor;

        animateReady = false;
        animateActive = false;

        ResetIconScale();
        DisableGlow();
    }

    private void ShowActive()
    {
        SetAlpha(cooldownFill, 0f);

        timerText.color = activeTextColor;

        animateReady = false;
        animateActive = true;

        ResetIconScale();
        icon.color = activeColor;

        EnableGlow(activeColor);
    }

    #endregion

    #region Helper Methods

    private void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private void PulseIcon()
    {
        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        icon.transform.localScale = originalScale * (1f + scaleOffset);
    }

    private void ResetIconScale()
    {
        icon.transform.localScale = originalScale;
    }

    private void AnimateReadyGlow()
    {
        if (readyGlow == null) return;

        Color c = readyGlow.effectColor;
        c.a = Mathf.Lerp(0f, glowMaxAlpha,
            (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f);

        readyGlow.effectColor = c;
    }

    private void EnableGlow(Color glowColor)
    {
        if (readyGlow == null) return;

        Color c = glowColor;
        c.a = readyGlow.effectColor.a;
        readyGlow.effectColor = c;
    }

    private void DisableGlow()
    {
        if (readyGlow == null) return;

        Color c = readyGlow.effectColor;
        c.a = 0f;
        readyGlow.effectColor = c;
    }

    #endregion

    #region Public Registration

    public void SetOverdriveAbility(OverdriveAbility ability)
    {
        _overdrive = ability;
    }

    #endregion

    #region View Handlers (MVVM Bindings)

    private void OnStateChanged(OverdriveState state, OverdriveState? previous)
    {
        switch (state)
        {
            case OverdriveState.Ready:
                ShowReady();
                break;

            case OverdriveState.Active:
                ShowActive();
                break;

            case OverdriveState.Cooldown:
                ShowCooldown();
                break;
        }
    }

    private void OnCooldownChanged(float time)
    {
        if (_overdrive.CurrentState != OverdriveState.Cooldown)
            return;

        timerText.text = $"{time:F1}s";
    }

    private void OnDurationChanged(float time)
    {
        if (_overdrive.CurrentState != OverdriveState.Active)
            return;

        timerText.text = $"{time:F1}s";

        if (time <= lowTimeWarningThreshold)
        {
            icon.color = lowTimeColor;
            EnableGlow(lowTimeColor);
        }
        else
        {
            icon.color = activeColor;
            EnableGlow(activeColor);
        }
    }

    private void OnCooldownFillChanged(float fill)
    {
        cooldownFill.fillAmount = fill;
    }

    #endregion
}