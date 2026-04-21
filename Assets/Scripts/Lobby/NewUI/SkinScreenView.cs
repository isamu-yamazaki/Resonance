using System;
using Resonance.Assemblies.UISystem;
using Resonance.PlayerController;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.LobbySystem.NewUI
{
    public class SkinScreenView : MonoBehaviour, IScreenView
    {
        [SerializeField] private SkinCatalog skinCatalog;
        [SerializeField] private Transform content;
        [SerializeField] private GameObject entryButtonPrefab;
        [SerializeField] private Button doneButton;
        private Action back;

        public static string Key => nameof(SkinScreenView);
        string IScreenView.Key => Key;

        private void HandleDoneClicked()
        {
            back?.Invoke();
        }

        public void OnShow(ScreenViewActions viewActions)
        {
            gameObject.SetActive(true);
            PopulateEntries();

            doneButton.onClick.AddListener(HandleDoneClicked);

            back = viewActions.Back;
        }

        public void OnHide()
        {
            gameObject.SetActive(false);
            doneButton.onClick.RemoveListener(HandleDoneClicked);

            back = null;
        }

        private void PopulateEntries()
        {
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < skinCatalog.Count; i++)
            {
                var entry = Instantiate(entryButtonPrefab, content);
                var button = entry.GetComponent<Button>();
                var index = i;
                button.onClick.AddListener(() => OnSkinSelected(index));
            }
        }

        private void OnSkinSelected(int selected)
        {
            var skinIndexProvider = FindFirstObjectByType<SkinIndexProvider>();
            if (!skinIndexProvider)
            {
                Debug.LogError($"[{GetType()}] No SkinIndexProvider object, cannot update skin index");
            }
            skinIndexProvider.SetSkinIndex(selected);
        }
    }
}
