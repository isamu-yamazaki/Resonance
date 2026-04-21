using Resonance.Assemblies.UISystem;
using UnityEngine;

namespace Resonance.LobbySystem.NewUI
{
    public class FriendOverlayView : MonoBehaviour, IOverlayView
    {
        public static string Key => nameof(FriendOverlayView);
        string IOverlayView.Key => Key;

        

        [SerializeField] private Transform friendsListContent;

        public void OnShow(OverlayViewActions viewActions)
        {
            gameObject.SetActive(true);
        }

        public void OnHide()
        {
            gameObject.SetActive(false);
        }
    }
}
