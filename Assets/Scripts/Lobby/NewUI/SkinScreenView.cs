using Resonance.Assemblies.UISystem;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.LobbySystem.NewUI
{
    public class SkinScreenView : MonoBehaviour, IScreenView
    {
        [SerializeField] private SkinCatalog skinCatalog;
        [SerializeField] private Transform content;

        public static string Key => nameof(SkinScreenView);
        string IScreenView.Key => Key;

        public void OnShow(ScreenViewActions viewActions)
        {
            gameObject.SetActive(true);
        }

        public void OnHide()
        {
            gameObject.SetActive(false);
        }
    }
}
