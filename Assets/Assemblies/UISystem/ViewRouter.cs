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

        public void ToggleOverlay(int id)
        {
            if (activeOverlayIds.Contains(id))
            {
                HideOverlay(id);
            } else
            {
                ShowOverlay(id);
            }
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
            OverlayViewActions viewActions = new()
            {
                Dismiss = () =>
                {
                    HideOverlay(id);
                },
                Id = id,
            };
            overlayOptions.view.OnShow(viewActions);

            RefreshCursorUnlocks();
            RefreshInputMaps();
        }

        public void HideOverlay(int id)
        {
            if (!activeOverlayIds.Contains(id)) return;
            if (!overlays.ContainsKey(id))
            {
                throw new KeyNotFoundException("Overlay ID not registered");
            }

            activeOverlayIds.Remove(id);
            overlays[id].view.OnHide();

            RefreshCursorUnlocks();
            RefreshInputMaps();
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

            // if view router gets extended to include a "primary view"
            // underneath the overlay system, we may need to change
            // cursor refresh behavior

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void RefreshInputMaps()
        {
            // if disabled input map is shared among both active/non-active overlay,
            // that input maps should still be disabled

            var inputMapsToDisable = GetInputMapsForOverlays(activeOverlayIds);
            var inputMapsToEnable = GetInputMapsForOverlays(NonActiveOverlayIds).Except(inputMapsToDisable);

            foreach (var inputMap in inputMapsToEnable)
            {
                inputMap.Enable();
            }
            foreach (var inputMap in inputMapsToDisable)
            {
                inputMap.Disable();
            }
        }

        private HashSet<InputActionMap> GetInputMapsForOverlays(IReadOnlyCollection<int> overlayIds)
        {
            HashSet<InputActionMap> inputMapsToDisable = new();
            foreach (var id in overlayIds)
            {
                if (overlays.ContainsKey(id))
                {
                    foreach (var inputMap in overlays[id].inputMapsToDisableWhenShown)
                    {
                        inputMapsToDisable.Add(inputMap);
                    }
                }
            }

            return inputMapsToDisable;
        }
    }
}
