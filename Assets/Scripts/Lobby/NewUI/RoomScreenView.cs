using Resonance.Assemblies.UISystem;
using UnityEngine;

namespace Resonance.LobbySystem.NewUI
{
    public class RoomScreenView : MonoBehaviour, IScreenView
    {
        public static string Key => nameof(RoomScreenView);
        string IScreenView.Key => Key;

        public void OnShow(ScreenViewActions viewActions)
        {
        }

        public void OnHide()
        {
        }
    }
}
