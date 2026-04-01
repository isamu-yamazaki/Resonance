using System;
using TMPro;
using UnityEngine;

namespace Resonance.LobbySystem.TemporaryUI
{
    public class RenderScaleDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        public void SetDisplayValue(float displayValue)
        {
            text.text = $"{Math.Round(displayValue, 3)}";
        }
    }
}
