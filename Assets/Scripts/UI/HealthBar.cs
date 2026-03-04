using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Resonance.Helper;

namespace Resonance.UI
{
    public class HealthBar : MonoBehaviour
    {
        [Header("UI References")]
        public Image healthFill;
        public Image damageFill;
        public TextMeshProUGUI healthText;

        [Header("Animation")]
        [SerializeField] float damageLerpSpeed = 2f;
        [SerializeField] float numberTickSpeed = 5f;

        [Header("Damage Flash")]
        [SerializeField] Color damageFlashColor = Color.red;
        [SerializeField] float flashDuration = 0.2f;

        private float maxHealth;
        private float displayedHealth;
        private float displayedDamageBar;
        private float targetHealth;

        private Coroutine flashRoutine;

        private PlayerViewModel viewModel;

        public void Bind(PlayerViewModel vm)
        {
            viewModel = vm;
            maxHealth = vm.MaxHealth;
            targetHealth = displayedHealth = displayedDamageBar = maxHealth;

            // Subscribe to changes
            vm.Health.ChangeEvent += OnHealthChanged;

            UpdateUIInstant();
        }

        private void OnHealthChanged(float newHealth)
        {
            if (newHealth < targetHealth)
            {
                if (flashRoutine != null)
                {
                    StopCoroutine(flashRoutine);
                    healthText.color = Color.white;
                }

                flashRoutine = StartCoroutine(FlashNumbers());
            }

            targetHealth = newHealth;
        }

        void Update()
        {
            if (maxHealth <= 0) return;

            // Smooth health bar animation
            displayedHealth = Mathf.Lerp(displayedHealth, targetHealth, Time.deltaTime * 15f);
            displayedDamageBar = Mathf.Lerp(displayedDamageBar, targetHealth, Time.deltaTime * damageLerpSpeed);

            UpdateUI();
        }

        void UpdateUI()
        {
            float healthPercent = displayedHealth / maxHealth;
            float damagePercent = displayedDamageBar / maxHealth;

            healthFill.fillAmount = healthPercent;
            damageFill.fillAmount = damagePercent;

            healthText.text = $"{Mathf.RoundToInt(displayedHealth)} / {Mathf.RoundToInt(maxHealth)}";
        }

        void UpdateUIInstant()
        {
            healthFill.fillAmount = 1f;
            damageFill.fillAmount = 1f;
            healthText.text = $"{maxHealth} / {maxHealth}";
        }

        IEnumerator FlashNumbers()
        {
            healthText.color = damageFlashColor;
            yield return new WaitForSeconds(flashDuration);
            healthText.color = Color.white;
        }

        private void OnDestroy()
        {
            if (viewModel != null)
            {
                viewModel.Health.ChangeEvent -= OnHealthChanged;
            }
        }
    }
}
