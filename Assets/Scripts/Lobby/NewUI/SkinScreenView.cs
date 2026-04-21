using System;
using Resonance.Assemblies.UISystem;
using Resonance.LobbySystem.DataProviders;
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

#if !UNITY_SERVER
        [Header("Wwise Events")]
        [SerializeField] private AK.Wwise.Event buttonClickEvent;
        [SerializeField] private AK.Wwise.Event skinSelectEvent;
#endif

        private Action back;

        public static string Key => nameof(SkinScreenView);
        string IScreenView.Key => Key;

#if !UNITY_SERVER
        private void PostClick(AK.Wwise.Event wwiseEvent)
        {
            if (wwiseEvent != null && wwiseEvent.IsValid())
                wwiseEvent.Post(gameObject);
        }
#endif

        private void HandleDoneClicked()
        {
#if !UNITY_SERVER
            PostClick(buttonClickEvent);
#endif
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
                Destroy(child.gameObject);

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
            var skinIndexProvider = SkinIndexProvider.Instance;

#if !UNITY_SERVER
            PostClick(skinSelectEvent);
#endif

            if (!skinIndexProvider)
            {
                Debug.LogError($"[{GetType()}] No SkinIndexProvider object, cannot update skin index");
                return;
            }
            skinIndexProvider.SetSkinIndex(selected);
        }
    }
}
