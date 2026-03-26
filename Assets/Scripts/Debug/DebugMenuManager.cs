using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.DebugTools
{
    public class DebugMenuManager : MonoBehaviour
    {
        #region Singleton
        public static DebugMenuManager Instance { get; private set; }
        #endregion

        #region Class Variables
        [Header("Settings")]
        [SerializeField] private bool showOnStart = false;

        public bool _showMenu = false;
        private Keyboard _keyboard;

        // Panels
        private PerformanceDebugPanel _performancePanel;
        private PlayerDebugPanel _playerPanel;
        private SceneDebugPanel _scenePanel;
#if !UNITY_SERVER
        private AudioDebugPanel _audioPanel;
#endif

        // Tab system
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Scene", "Performance", "Player", "Audio" };
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
            DontDestroyOnLoad(gameObject);

            _keyboard = Keyboard.current;
            _showMenu = showOnStart;

            // Add panels
            _scenePanel = gameObject.AddComponent<SceneDebugPanel>();
            _performancePanel = gameObject.AddComponent<PerformanceDebugPanel>();
            _playerPanel = gameObject.AddComponent<PlayerDebugPanel>();
#if !UNITY_SERVER
            _audioPanel = gameObject.AddComponent<AudioDebugPanel>();
#endif
        }
        #endregion

        #region Update
        private void Update()
        {
            if (_keyboard != null && _keyboard.f1Key.wasPressedThisFrame)
            {
                _showMenu = !_showMenu;

                if (_showMenu)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
        #endregion

        #region OnGUI
        private void OnGUI()
        {
            if (!_showMenu) return;

            GUI.Window(0, new Rect(50, 50, 450, 700), DrawDebugWindow, "Resonance Debug Menu");
        }

        private void DrawDebugWindow(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Press F1 to toggle menu");
            GUILayout.Space(10);

            // Tab selection
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            GUILayout.Space(10);

            // Draw active panel
            switch (_selectedTab)
            {
                case 0: // Scene
                    _scenePanel.DrawPanel();
                    break;
                case 1: // Performance
                    _performancePanel.DrawPanel();
                    break;
                case 2: // Player
                    _playerPanel.DrawPanel();
                    break;
                case 3: // Audio
#if !UNITY_SERVER
                    _audioPanel.DrawPanel();
#endif
                    break;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close Menu", GUILayout.Height(30)))
            {
                _showMenu = false;
            }

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
        #endregion
    }
}
