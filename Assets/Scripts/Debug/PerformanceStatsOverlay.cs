using System;
using PurrNet;
using Resonance.Assemblies.UISystem;
using UnityEngine;

namespace Resonance.DebugTools
{
    public class PerformanceStatsOverlay : MonoBehaviour, IOverlayView
    {
        public static string Key => nameof(PerformanceStatsOverlay);
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
        [SerializeField] private float goodMemoryMB = 800f;
        [SerializeField] private float warningMemoryMB = 1200f;
        [SerializeField] private float updateInterval = 0.5f;

        [Header("Network Thresholds (ms)")]
        [SerializeField] private int goodPingMs = 60;
        [SerializeField] private int warningPingMs = 120;

        private const float BOX_WIDTH = 150f;

        private bool _isVisible;
        private Action _dismiss;
        private float _currentFPS;
        private float _currentMs;
        private float _worstMs;
        private float _bestMs = float.MaxValue;

        private int _frameCount;
        private float _fpsAccumulator;
        private float _timeSinceUpdate;

        private GUIStyle _fpsStyle;
        private GUIStyle _msStyle;
        private GUIStyle _memoryStyle;
        private GUIStyle _smallTextStyle;

        private float _lineHeight;
        private float _smallLineHeight;

        private StatisticsManager _statisticsManager;

        private void Start()
        {
            _statisticsManager = FindObjectOfType<StatisticsManager>();
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

            _msStyle = new GUIStyle
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = Color.white }
            };

            _memoryStyle = new GUIStyle
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = Color.white }
            };

            _smallTextStyle = new GUIStyle
            {
                fontSize = fontSize - 6,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = Color.white }
            };
        }

        private void CalculateLayout()
        {
            _lineHeight = fontSize + 2;
            _smallLineHeight = (fontSize - 6) + 2;
        }

        #region IOverlayView
        public void OnShow(OverlayViewActions viewActions)
        {
            _isVisible = true;
            _dismiss = viewActions.Dismiss;
        }

        public void OnHide()
        {
            _isVisible = false;
            _dismiss = null;
        }
        #endregion

        private void Update()
        {
            if (!_isVisible) return;

            UpdatePerformanceMetrics();
        }

        private void UpdatePerformanceMetrics()
        {
            float currentFrameMs = Time.unscaledDeltaTime * 1000f;

            if (currentFrameMs > _worstMs)
                _worstMs = currentFrameMs;

            if (currentFrameMs < _bestMs && currentFrameMs > 0)
                _bestMs = currentFrameMs;

            _timeSinceUpdate += Time.unscaledDeltaTime;
            _fpsAccumulator += Time.timeScale / Time.unscaledDeltaTime;
            _frameCount++;

            if (_timeSinceUpdate >= updateInterval)
            {
                _currentFPS = _fpsAccumulator / _frameCount;
                _currentMs = 1000f / _currentFPS;

                _timeSinceUpdate = 0f;
                _fpsAccumulator = 0f;
                _frameCount = 0;
            }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            UpdateTextColor();
            DrawStatsPanel();
        }

        private void UpdateTextColor()
        {
            // FPS color (higher is better)
            _fpsStyle.normal.textColor = _currentFPS >= goodFPS ? goodColor :
                                         _currentFPS >= warningFPS ? warningColor : badColor;

            // MS color (lower is better)
            _msStyle.normal.textColor = _currentMs <= (1000f / goodFPS) ? goodColor :
                                        _currentMs <= (1000f / warningFPS) ? warningColor : badColor;

            // Memory color (lower is better)
            float memoryMB = System.GC.GetTotalMemory(false) / 1048576f;
            _memoryStyle.normal.textColor = memoryMB <= goodMemoryMB ? goodColor :
                                            memoryMB <= warningMemoryMB ? warningColor : badColor;
        }

        private void DrawStatsPanel()
        {
            float xPos = Screen.width - BOX_WIDTH - rightMargin;
            float yPos = topMargin;
            float memoryMB = System.GC.GetTotalMemory(false) / 1048576f;

            // Draw FPS
            GUI.Label(new Rect(xPos, yPos, BOX_WIDTH, _lineHeight), $"{_currentFPS:0.} FPS", _fpsStyle);

            // Draw current ms
            float msYPos = yPos + _lineHeight;
            GUI.Label(new Rect(xPos, msYPos, BOX_WIDTH, _lineHeight), $"{_currentMs:0.0} ms", _msStyle);

            // Draw worst/best ms in smaller font (white)
            float smallStatsYPos = msYPos + _lineHeight;
            string smallStats = $"{_worstMs:0.0} ms worst\n{_bestMs:0.0} ms best";
            GUI.Label(new Rect(xPos, smallStatsYPos, BOX_WIDTH, _smallLineHeight * 2), smallStats, _smallTextStyle);

            // Draw memory at bottom
            float memoryYPos = smallStatsYPos + (_smallLineHeight * 2);
            GUI.Label(new Rect(xPos, memoryYPos, BOX_WIDTH, _lineHeight), $"{memoryMB:0.0} MB", _memoryStyle);

            if (_statisticsManager != null)
            {
                float netYPos = memoryYPos + _lineHeight + 4f;
                DrawNetworkStats(xPos, netYPos);
            }
        }

        private void DrawNetworkStats(float xPos, float yPos)
        {
            GUI.Label(new Rect(xPos, yPos, BOX_WIDTH, _smallLineHeight), "NET", _smallTextStyle);
            yPos += _smallLineHeight;

            string clientLabel = _statisticsManager.connectedClient ? "Client: on" : "Client: off";
            string serverLabel = _statisticsManager.connectedServer ? "  Server: on" : "  Server: off";
            GUI.Label(new Rect(xPos, yPos, BOX_WIDTH, _smallLineHeight), clientLabel + serverLabel, _smallTextStyle);
            yPos += _smallLineHeight;

            if (_statisticsManager.connectedClient)
            {
                var pingStyle = new GUIStyle(_smallTextStyle);
                pingStyle.normal.textColor = _statisticsManager.ping <= goodPingMs ? goodColor :
                                             _statisticsManager.ping <= warningPingMs ? warningColor : badColor;
                GUI.Label(new Rect(xPos, yPos, BOX_WIDTH, _smallLineHeight), $"Ping: {_statisticsManager.ping}ms", pingStyle);
                yPos += _smallLineHeight;

                GUI.Label(new Rect(xPos, yPos, BOX_WIDTH, _smallLineHeight), $"Jitter: {_statisticsManager.jitter}ms", _smallTextStyle);
                yPos += _smallLineHeight;

                GUI.Label(new Rect(xPos, yPos, BOX_WIDTH, _smallLineHeight), $"Loss: {_statisticsManager.packetLoss}%", _smallTextStyle);
                yPos += _smallLineHeight;

                GUI.Label(new Rect(xPos, yPos, BOX_WIDTH, _smallLineHeight), $"Up: {_statisticsManager.upload:F1} KB/s", _smallTextStyle);
                yPos += _smallLineHeight;

                GUI.Label(new Rect(xPos, yPos, BOX_WIDTH, _smallLineHeight), $"Down: {_statisticsManager.download:F1} KB/s", _smallTextStyle);
            }
            else
            {
                GUI.Label(new Rect(xPos, yPos, BOX_WIDTH, _smallLineHeight), "not connected", _smallTextStyle);
            }
        }
    }
}
