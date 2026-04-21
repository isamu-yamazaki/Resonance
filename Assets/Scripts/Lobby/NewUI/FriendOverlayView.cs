using Resonance.Assemblies.UISystem;
using UnityEngine;

namespace Resonance.LobbySystem.NewUI
{
    public class FriendOverlayView : MonoBehaviour, IOverlayView
    {
        public static string Key => nameof(FriendOverlayView);
        string IOverlayView.Key => Key;

        public void OnShow(OverlayViewActions viewActions)
        {
        }

        public void OnHide()
        {
        }
    }
}
