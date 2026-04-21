using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.Assemblies.UISystem
{
    public class ViewRouter
    {
        private Dictionary<string, OverlayOptions> overlays = new();
        private HashSet<string> activeOverlayKeys = new();

        public IReadOnlyDictionary<string, OverlayOptions> Overlays => overlays;
        public IReadOnlyCollection<string> ActiveOverlayKeys => activeOverlayKeys;
        public IReadOnlyCollection<string> NonActiveOverlayKeys => overlays.Keys.Except(activeOverlayKeys).ToHashSet();

        private Dictionary<string, ScreenViewOptions> screenViews = new();
        private List<string> history = new();

        public IReadOnlyDictionary<string, ScreenViewOptions> ScreenViews => screenViews;
        public IReadOnlyList<string> ScreenViewHistory => history;
        public string ActiveScreenViewKey => history.Count > 0 ? history[history.Count - 1] : null;

        // overlays: because this just toggles on/off existing game objects, z-ordering
        // is done in the Unity editor. screen views: exactly one is visible at a time
        // (the top of the stack), so there is no z-ordering problem to solve.

        public void RegisterOverlay(OverlayOptions options)
        {
            if (options.view == null)
            {
                throw new ArgumentNullException("OverlayOptions is missing a view!");
            }
            var key = options.view.Key;
            if (overlays.ContainsKey(key))
            {
                throw new ArgumentException($"Overlay with key '{key}' already registered");
            }
            overlays.Add(key, options);
        }

        public void ToggleOverlay(string key)
        {
            if (activeOverlayKeys.Contains(key))
            {
                HideOverlay(key);
            } else
            {
                ShowOverlay(key);
            }
        }

        public void ShowOverlay(string key)
        {
            if (!overlays.ContainsKey(key))
            {
                throw new KeyNotFoundException("Overlay key not registered");
            }
            if (activeOverlayKeys.Contains(key)) return;

            OverlayOptions overlayOptions = overlays[key];
            activeOverlayKeys.Add(key);

            OverlayViewActions viewActions = new()
            {
                Dismiss = () =>
                {
                    HideOverlay(key);
                },
            };
            overlayOptions.view.OnShow(viewActions);

            RefreshCursorUnlocks();
            RefreshInputMaps();
        }

        public void HideOverlay(string key)
        {
            if (!overlays.ContainsKey(key))
            {
                throw new KeyNotFoundException("Overlay key not registered");
            }
            if (!activeOverlayKeys.Contains(key)) return;

            activeOverlayKeys.Remove(key);
            overlays[key].view.OnHide();

            RefreshCursorUnlocks();
            RefreshInputMaps();
        }

        public void RegisterScreenView(ScreenViewOptions options)
        {
            if (options.view == null)
            {
                throw new ArgumentNullException("ScreenViewOptions is missing a view!");
            }
            var key = options.view.Key;
            if (screenViews.ContainsKey(key))
            {
                throw new ArgumentException($"Screen view with key '{key}' already registered");
            }
            screenViews.Add(key, options);
        }

        public void PushScreenView(string key)
        {
            if (!screenViews.ContainsKey(key))
            {
                throw new KeyNotFoundException("Screen view key not registered");
            }
            if (history.Count > 0 && history[history.Count - 1] == key) return;

            if (history.Count > 0)
            {
                var prevKey = history[history.Count - 1];
                screenViews[prevKey].view.OnHide();
            }

            history.Add(key);
            InvokeOnShowForTop();

            RefreshCursorUnlocks();
            RefreshInputMaps();
        }

        public void PopScreenView()
        {
            if (history.Count == 0) return;

            var topKey = history[history.Count - 1];
            screenViews[topKey].view.OnHide();
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

            var topKey = history[history.Count - 1];
            screenViews[topKey].view.OnHide();
            history.Clear();

            RefreshCursorUnlocks();
            RefreshInputMaps();
        }

        private void InvokeOnShowForTop()
        {
            var topKey = history[history.Count - 1];
            ScreenViewActions actions = new()
            {
                Back = history.Count > 1 ? () => PopScreenView() : null,
            };
            screenViews[topKey].view.OnShow(actions);
        }

        private void RefreshCursorUnlocks()
        {
            foreach (var key in activeOverlayKeys)
            {
                if (overlays.ContainsKey(key) && overlays[key].unlockCursorWhenShown)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    return;
                }
            }

            if (ActiveScreenViewKey != null)
            {
                var topKey = ActiveScreenViewKey;
                if (screenViews.ContainsKey(topKey) && screenViews[topKey].unlockCursorWhenShown)
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

            var activeScreenKeys = ActiveScreenViewKey != null
                ? new[] { ActiveScreenViewKey }
                : Array.Empty<string>();
            var nonActiveScreenKeys = screenViews.Keys.Except(activeScreenKeys).ToHashSet();

            var inputMapsToDisable = GetInputMapsForOverlays(activeOverlayKeys);
            inputMapsToDisable.UnionWith(GetInputMapsForScreenViews(activeScreenKeys));

            var inputMapsToEnable = GetInputMapsForOverlays(NonActiveOverlayKeys);
            inputMapsToEnable.UnionWith(GetInputMapsForScreenViews(nonActiveScreenKeys));
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

        private HashSet<InputActionMap> GetInputMapsForOverlays(IReadOnlyCollection<string> overlayKeys)
        {
            HashSet<InputActionMap> inputMapsToDisable = new();
            foreach (var key in overlayKeys)
            {
                if (overlays.ContainsKey(key))
                {
                    foreach (var inputMap in overlays[key].inputMapsToDisableWhenShown)
                    {
                        inputMapsToDisable.Add(inputMap);
                    }
                }
            }

            return inputMapsToDisable;
        }

        private HashSet<InputActionMap> GetInputMapsForScreenViews(IReadOnlyCollection<string> screenViewKeys)
        {
            HashSet<InputActionMap> result = new();
            foreach (var key in screenViewKeys)
            {
                if (screenViews.ContainsKey(key))
                {
                    foreach (var inputMap in screenViews[key].inputMapsToDisableWhenShown)
                    {
                        result.Add(inputMap);
                    }
                }
            }

            return result;
        }
    }
}
