using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.Assemblies.UISystem
{
    public class ViewRouter
    {
        private Dictionary<int, OverlayOptions> overlays = new();
        private HashSet<int> activeOverlayIds = new();

        public IReadOnlyDictionary<int, OverlayOptions> Overlays => overlays;
        public IReadOnlyCollection<int> ActiveOverlayIds => activeOverlayIds;
        public IReadOnlyCollection<int> NonActiveOverlayIds => overlays.Keys.Except(activeOverlayIds).ToHashSet();

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

            OverlayOptions overlayOptions = overlays[id];
            overlayOptions.view.Show();

            RefreshCursorUnlocks();

            foreach (var inputMap in overlayOptions.inputMapsToDisableWhenShown)
            {
                inputMap.Disable();
            }
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

            RefreshCursorUnlocks();

            foreach (var inputMap in overlays[id].inputMapsToDisableWhenShown)
            {
                inputMap.Enable();
            }
        }

        private void RefreshCursorUnlocks()
        {
            foreach (var id in activeOverlayIds)
            {
                if (overlays.ContainsKey(id) && overlays[id].unlockCursorWhenShown)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    return;
                }
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
