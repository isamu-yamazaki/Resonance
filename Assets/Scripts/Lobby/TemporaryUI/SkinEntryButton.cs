using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.LobbySystem.TemporaryUI
{
    [RequireComponent(typeof(Button))]
    public class SkinEntryButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text skinNameText;

        private Action<int> _onSelected;
        private int _index;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        public void Init(string skinName, int index, Action<int> onSelected)
        {
            skinNameText.text = skinName;
            _index = index;
            _onSelected = onSelected;
        }

        private void OnClick()
        {
            _onSelected?.Invoke(_index);
        }
    }
}
