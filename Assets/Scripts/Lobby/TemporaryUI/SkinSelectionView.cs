using System;
using Resonance.PlayerController;
using UnityEngine;
using UnityEngine.Events;

namespace Resonance.LobbySystem.TemporaryUI
{
    public class SkinSelectionView : View
    {
        [SerializeField] private Transform content;
        [SerializeField] private SkinEntryButton entryButtonPrefab;
        [SerializeField] private SkinCatalog skinCatalog;

        public void Show()
        {
            PopulateEntries();
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
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
                var index = i;
                entry.Init(skinCatalog.Get(i).skinName, i,
                    selected => OnSkinSelected(selected));
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
