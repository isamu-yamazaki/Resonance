using System;
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

        private Dictionary<int, ScreenViewOptions> screenViews = new();
        private List<int> history = new();

        public IReadOnlyDictionary<int, ScreenViewOptions> ScreenViews => screenViews;
        public IReadOnlyList<int> ScreenViewHistory => history;
        public int? ActiveScreenViewId => history.Count > 0 ? history[history.Count - 1] : (int?)null;

        // overlays: because this just toggles on/off existing game objects, z-ordering
        // is done in the Unity editor. screen views: exactly one is visible at a time
        // (the top of the stack), so there is no z-ordering problem to solve.

        private int nextId = 0;

        private int GetNewId()
        {
            nextId++;
            return nextId;
        }

        public int RegisterOverlay(OverlayOptions options)
        {
            if (options.view == null)
            {
                throw new ArgumentNullException("OverlayOptions is missing a view!");
            }
            var id = GetNewId();
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

            OverlayOptions overlayOptions = overlays[id];
            activeOverlayIds.Add(id);

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

        public int RegisterScreenView(ScreenViewOptions options)
        {
            if (options.view == null)
            {
                throw new ArgumentNullException("ScreenViewOptions is missing a view!");
            }
            var id = GetNewId();
            screenViews.Add(id, options);
            return id;
        }

        public void PushScreenView(int id)
        {
            if (!screenViews.ContainsKey(id))
            {
                throw new KeyNotFoundException("Screen view ID not registered");
            }
            if (history.Count > 0 && history[history.Count - 1] == id) return;

            if (history.Count > 0)
            {
                var prevId = history[history.Count - 1];
                screenViews[prevId].view.OnHide();
            }

            history.Add(id);
            InvokeOnShowForTop();

            RefreshCursorUnlocks();
            RefreshInputMaps();
        }

        public void PopScreenView()
        {
            if (history.Count == 0) return;

            var topId = history[history.Count - 1];
            screenViews[topId].view.OnHide();
            history.RemoveAt(history.Count - 1);

            if (history.Count > 0)
            {
                InvokeOnShowForTop();
            }

            RefreshCursorUnlocks();
            RefreshInputMaps();
        }

        public void PopAllScreenViews()
        {
            if (history.Count == 0) return;

            var topId = history[history.Count - 1];
            screenViews[topId].view.OnHide();
            history.Clear();

            RefreshCursorUnlocks();
            RefreshInputMaps();
        }

        private void InvokeOnShowForTop()
        {
            var topId = history[history.Count - 1];
            ScreenViewActions actions = new()
            {
                Id = topId,
                Back = history.Count > 1 ? () => PopScreenView() : null,
            };
            screenViews[topId].view.OnShow(actions);
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

            if (ActiveScreenViewId.HasValue)
            {
                var topId = ActiveScreenViewId.Value;
                if (screenViews.ContainsKey(topId) && screenViews[topId].unlockCursorWhenShown)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    return;
                }
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void RefreshInputMaps()
        {
            // if a disabled input map is shared between any active source and any
            // non-active source (overlay or screen), that input map should stay disabled

            var activeScreenIds = ActiveScreenViewId.HasValue
                ? new[] { ActiveScreenViewId.Value }
                : Array.Empty<int>();
            var nonActiveScreenIds = screenViews.Keys.Except(activeScreenIds).ToHashSet();

            var inputMapsToDisable = GetInputMapsForOverlays(activeOverlayIds);
            inputMapsToDisable.UnionWith(GetInputMapsForScreenViews(activeScreenIds));

            var inputMapsToEnable = GetInputMapsForOverlays(NonActiveOverlayIds);
            inputMapsToEnable.UnionWith(GetInputMapsForScreenViews(nonActiveScreenIds));
            inputMapsToEnable.ExceptWith(inputMapsToDisable);

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

        private HashSet<InputActionMap> GetInputMapsForScreenViews(IReadOnlyCollection<int> screenViewIds)
        {
            HashSet<InputActionMap> result = new();
            foreach (var id in screenViewIds)
            {
                if (screenViews.ContainsKey(id))
                {
                    foreach (var inputMap in screenViews[id].inputMapsToDisableWhenShown)
                    {
                        result.Add(inputMap);
                    }
                }
            }

            return result;
        }
    }
}
