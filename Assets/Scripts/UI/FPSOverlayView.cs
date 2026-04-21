using Resonance.Assemblies.UISystem;
using UnityEngine;

namespace Resonance.UI
{
    public class FPSOverlayView : MonoBehaviour, IOverlayView
    {
        public static string Key => nameof(FPSOverlayView);
        string IOverlayView.Key => Key;

        [Header("Display Settings")]
        [SerializeField] private int fontSize = 20;
        [SerializeField] private float rightMargin = 10f;
        [SerializeField] private float topMargin = 10f;
        [SerializeField] private Color goodColor = new Color(0.2f, 1f, 0.2f);
        [SerializeField] private Color warningColor = new Color(1f, 1f, 0.2f);
        [SerializeField] private Color badColor = new Color(1f, 0.2f, 0.2f);

        [Header("Performance Thresholds")]
        [SerializeField] private float goodFPS = 60f;
        [SerializeField] private float warningFPS = 30f;
        [SerializeField] private float updateInterval = 0.5f;

        private const float BOX_WIDTH = 150f;

        private bool _isVisible;
        private float _currentFPS;
        private int _frameCount;
        private float _fpsAccumulator;
        private float _timeSinceUpdate;

        private GUIStyle _fpsStyle;
        private float _lineHeight;

        private void Start()
        {
            InitializeStyles();
            CalculateLayout();
        }

        private void InitializeStyles()
        {
            _fpsStyle = new GUIStyle
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = Color.white }
            };
        }

        private void CalculateLayout()
        {
            _lineHeight = fontSize + 2;
        }

        public void OnShow(OverlayViewActions viewActions)
        {
            _isVisible = true;
        }

        public void OnHide()
        {
            _isVisible = false;
        }

        private void Update()
        {
            if (!_isVisible) return;

            UpdatePerformanceMetrics();
        }

        private void UpdatePerformanceMetrics()
        {
            _timeSinceUpdate += Time.unscaledDeltaTime;
            _fpsAccumulator += Time.timeScale / Time.unscaledDeltaTime;
            _frameCount++;

            if (_timeSinceUpdate >= updateInterval)
            {
                _currentFPS = _fpsAccumulator / _frameCount;

                _timeSinceUpdate = 0f;
                _fpsAccumulator = 0f;
                _frameCount = 0;
            }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            UpdateTextColor();

            float xPos = Screen.width - BOX_WIDTH - rightMargin;
            GUI.Label(new Rect(xPos, topMargin, BOX_WIDTH, _lineHeight), $"{_currentFPS:0.} FPS", _fpsStyle);
        }

        private void UpdateTextColor()
        {
            _fpsStyle.normal.textColor = _currentFPS >= goodFPS ? goodColor :
                                         _currentFPS >= warningFPS ? warningColor : badColor;
        }
    }
}
