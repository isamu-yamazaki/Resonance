using Resonance.Assemblies.UISystem;
using UnityEngine;

namespace Resonance.LobbySystem.NewUI
{
    public class SkinScreenView : MonoBehaviour, IScreenView
    {
        public static string Key => nameof(SkinScreenView);
        string IScreenView.Key => Key;

        public void OnShow(ScreenViewActions viewActions)
        {
        }

        public void OnHide()
        {
        }
    }
}
