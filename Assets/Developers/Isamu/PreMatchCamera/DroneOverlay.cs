using UnityEngine;

namespace Resonance
{
    public class DroneOverlay : MonoBehaviour
    {
        [Header("Corner Brackets")]
        public float bracketSize = 40f;
        public float bracketThickness = 2f;
        public float bracketInset = 24f;

        [Header("Recording Dot")]
        public float dotRadius = 7f;
        public float blinkInterval = 0.6f;
        public Vector2 dotPosition = new Vector2(48f, 48f);

        [Header("Colors")]
        public Color bracketColor = new Color(1f, 1f, 1f, 0.6f);
        public Color recordColor = new Color(0.9f, 0.15f, 0.15f, 1f);
        public Color labelColor = new Color(1f, 1f, 1f, 0.5f);

        private Texture2D _whiteTexture;
        private float _blinkTimer = 0f;
        private bool _dotVisible = true;
        private GUIStyle _labelStyle;

        private void Awake()
        {
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void OnDestroy()
        {
            if (_whiteTexture != null)
                Destroy(_whiteTexture);
        }

        private void Update()
        {
            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= blinkInterval)
            {
                _blinkTimer = 0f;
                _dotVisible = !_dotVisible;
            }
        }

        private void OnGUI()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Normal,
                };
            }

            float w = Screen.width;
            float h = Screen.height;

            DrawCornerBrackets(w, h);
            DrawRecordingDot(w, h);
            DrawLabels(w, h);
        }

        private void DrawCornerBrackets(float w, float h)
        {
            float x0 = bracketInset;
            float y0 = bracketInset;
            float x1 = w - bracketInset;
            float y1 = h - bracketInset;
            float s = bracketSize;
            float t = bracketThickness;

            // Top-left
            DrawRect(x0, y0, s, t, bracketColor);
            DrawRect(x0, y0, t, s, bracketColor);

            // Top-right
            DrawRect(x1 - s, y0, s, t, bracketColor);
            DrawRect(x1 - t, y0, t, s, bracketColor);

            // Bottom-left
            DrawRect(x0, y1 - t, s, t, bracketColor);
            DrawRect(x0, y1 - s, t, s, bracketColor);

            // Bottom-right
            DrawRect(x1 - s, y1 - t, s, t, bracketColor);
            DrawRect(x1 - t, y1 - s, t, s, bracketColor);
        }

        private void DrawRecordingDot(float w, float h)
        {
            if (!_dotVisible)
                return;

            float x = dotPosition.x - dotRadius;
            float y = dotPosition.y - dotRadius;
            float size = dotRadius * 2f;

            DrawRect(x, y, size, size, recordColor);
        }

        private void DrawLabels(float w, float h)
        {
            _labelStyle.normal.textColor = labelColor;
            _labelStyle.alignment = TextAnchor.UpperLeft;
            GUI.Label(new Rect(dotPosition.x + dotRadius + 6f, dotPosition.y - 7f, 80f, 20f), "REC", _labelStyle);

            _labelStyle.alignment = TextAnchor.LowerRight;
            GUI.Label(new Rect(w - bracketInset - 95f, h - bracketInset - 36f, 80f, 20f), "DRONE_CAM", _labelStyle);
        }

        private void DrawRect(float x, float y, float w, float h, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, h), _whiteTexture);
            GUI.color = Color.white;
        }
    }
}
