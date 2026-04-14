using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Assemblies.UISystem
{
    public abstract class ViewRouter : MonoBehaviour
    {
        private Dictionary<int, OverlayOptions> overlays = new();
        private HashSet<int> activeOverlayIds = new();

        // note that because this just toggles on/off existing game objects,
        // z-ordering is done in the Unity editor

        private int nextOverlayId = 0;

        private int GetNewOverlayId()
        {
            nextOverlayId++;
            return nextOverlayId;
        }

        public int RegisterOverlay(OverlayOptions options)
        {
            var id = GetNewOverlayId();
            overlays.Add(id, options);
            return id;
        }

        public void ShowOverlay(int id)
        {
            if (activeOverlayIds.Contains(id)) return;
            if (!overlays.ContainsKey(id))
            {
                throw new KeyNotFoundException("Overlay ID not registered");
            }

            activeOverlayIds.Add(id);
            overlays[id].view.Show();
        }

        public void HideOverlay(int id)
        {
            if (!activeOverlayIds.Contains(id)) return;
            if (!overlays.ContainsKey(id))
            {
                throw new KeyNotFoundException("Overlay ID not registered");
            }

            activeOverlayIds.Remove(id);
            overlays[id].view.Hide();
        }
    }
}
