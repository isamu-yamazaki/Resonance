using System;
using Resonance.Assemblies.UISystem;
using UnityEngine;

namespace Resonance.DebugTools
{
    public class DebugOverlayView : MonoBehaviour, IOverlayView
    {
        public static string Key => nameof(DebugOverlayView);
        string IOverlayView.Key => Key;

        #region Singleton
        public static DebugOverlayView Instance { get; private set; }
        #endregion

        #region Class Variables
        [Header("Settings")]
        [SerializeField] private bool showOnStart = false;

        private bool showMenu = false;
        private Action dismiss;

        // Panels
        private PerformanceDebugPanel performancePanel;
        private PlayerDebugPanel playerPanel;
        private SceneDebugPanel scenePanel;
#if !UNITY_SERVER
        private AudioDebugPanel audioPanel;
#endif
        private MatchDebugPanel matchPanel;

        // Tab system
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Scene", "Performance", "Player", "Audio", "Match" };
        #endregion

        #region Startup
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            showMenu = showOnStart;

            // Add panels
            scenePanel = gameObject.AddComponent<SceneDebugPanel>();
            performancePanel = gameObject.AddComponent<PerformanceDebugPanel>();
            playerPanel = gameObject.AddComponent<PlayerDebugPanel>();
#if !UNITY_SERVER
            audioPanel = gameObject.AddComponent<AudioDebugPanel>();
#endif
            matchPanel = gameObject.AddComponent<MatchDebugPanel>();
        }
        #endregion

        #region IOverlayView
        public void OnShow(OverlayViewActions viewActions)
        {
            showMenu = true;
            dismiss = viewActions.Dismiss;
        }

        public void OnHide()
        {
            showMenu = false;
            dismiss = null;
        }

        public void Close()
        {
            dismiss?.Invoke();
        }
        #endregion

        #region OnGUI
        private void OnGUI()
        {
            if (!showMenu) return;

            GUI.Window(0, new Rect(50, 50, 450, 700), DrawDebugWindow, "Resonance Debug Menu");
        }

        private void DrawDebugWindow(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Press F1 to toggle menu");
            GUILayout.Space(10);

            // Tab selection
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            GUILayout.Space(10);

            // Draw active panel
            switch (selectedTab)
            {
                case 0: // Scene
                    scenePanel.DrawPanel();
                    break;
                case 1: // Performance
                    performancePanel.DrawPanel();
                    break;
                case 2: // Player
                    playerPanel.DrawPanel();
                    break;
                case 3: // Audio
#if !UNITY_SERVER
                    audioPanel.DrawPanel();
#endif
                    break;
                case 4:
                    matchPanel.DrawPanel();
                    break;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close Menu", GUILayout.Height(30)))
            {
                Close();
            }

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
        #endregion
    }
}
