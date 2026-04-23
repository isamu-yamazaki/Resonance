using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.Combat.Augments.UI
{
    public class AugmentSlotUI : MonoBehaviour
    {
        [Header("Slot Config")]
        [SerializeField] private AugmentSlot slotType;

        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownRadial;
        [SerializeField] private GameObject cooldownOverlay;
        [SerializeField] private TMPro.TextMeshProUGUI cooldownText;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private TMPro.TextMeshProUGUI keybindText;

        private IAugmentAbility _ability;
        private Coroutine _cdCoroutine;

        public void SetAugment(AugmentProperties augment, IAugmentAbility ability)
        {
            _ability = ability;

            emptyState.SetActive(false);
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = augment.Icon;

            cooldownText.text = string.Empty;
            cooldownOverlay.SetActive(false);
            cooldownRadial.fillAmount = 0f;
        }

        public void ClearAugment()
        {
            if (_cdCoroutine != null) StopCoroutine(_cdCoroutine);
            _ability = null;

            emptyState.SetActive(true);
            iconImage.gameObject.SetActive(false);
            cooldownText.text = string.Empty;
            cooldownOverlay.SetActive(false);
        }

        public void OnAbilityActivated()
        {
            if (_ability == null) return;
            if (_cdCoroutine != null) StopCoroutine(_cdCoroutine);
            _cdCoroutine = StartCoroutine(TickCooldown());
        }

        private IEnumerator TickCooldown()
        {
            cooldownOverlay.SetActive(true);

            while (_ability != null && _ability.CurrentCooldown > 0f)
            {
                float fraction = _ability.CurrentCooldown / _ability.MaxCooldown;
                cooldownRadial.fillAmount = fraction;
                cooldownText.text = Mathf.CeilToInt(_ability.CurrentCooldown).ToString();
                yield return null;
            }

            cooldownRadial.fillAmount = 0f;
            cooldownOverlay.SetActive(false);
        }
        
        public void SetKeybindLabel(string label)
        {
            if (keybindText != null)
                keybindText.text = label.ToUpper();
        }
    }
}