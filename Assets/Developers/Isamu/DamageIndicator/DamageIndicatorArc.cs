using UnityEngine;
using UnityEngine.UI;

namespace Resonance.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class DamageIndicatorArc : Graphic
    {
        [Header("Arc Shape")]
        public float innerRadius = 190f;
        public float outerRadius = 210f;
        public float arcDegrees = 60f;
        public int segments = 24;

        [Header("Appearance")]
        public Color arcColor = new Color(1f, 0.1f, 0.1f, 1f);
        public float fadeDuration = 1.5f;

        private float _timer = 0f;
        private float _angle = 0f;
        private bool _active = false;

        public float Angle => _angle;
        public bool IsActive => _active;

        protected override void Awake()
        {
            base.Awake();
            color = arcColor;
            gameObject.SetActive(false);
        }

        #region Activation

        public void Activate(float angle)
        {
            _angle = angle;
            _timer = 0f;
            _active = true;

            gameObject.SetActive(true);
            SetAlpha(1f);

            transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        public void Refresh(float angle)
        {
            _angle = angle;
            _timer = 0f;
            SetAlpha(1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        #endregion

        #region Update

        private void Update()
        {
            if (!_active) return;

            _timer += Time.deltaTime;

            float alpha = Mathf.Clamp01(1f - (_timer / fadeDuration));
            SetAlpha(alpha);

            if (_timer >= fadeDuration)
                Deactivate();
        }

        private void Deactivate()
        {
            _active = false;
            gameObject.SetActive(false);
        }

        private void SetAlpha(float alpha)
        {
            Color c = color;
            c.a = alpha;
            color = c;
            SetVerticesDirty();
        }

        #endregion

        #region Mesh Generation

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            float halfArc = arcDegrees * 0.5f;
            float startAngle = 90f - halfArc;
            float endAngle = 90f + halfArc;
            float stepSize = (endAngle - startAngle) / segments;

            for (int i = 0; i <= segments; i++)
            {
                float angleRad = Mathf.Deg2Rad * (startAngle + stepSize * i);
                float cos = Mathf.Cos(angleRad);
                float sin = Mathf.Sin(angleRad);

                Vector2 inner = new Vector2(cos * innerRadius, sin * innerRadius);
                Vector2 outer = new Vector2(cos * outerRadius, sin * outerRadius);

                // t=0 at edges, t=1 at center — drives side fade
                float t = 1f - Mathf.Abs((float)i / segments * 2f - 1f);
                float sideAlpha = color.a * t;

                Color edgeColor = new Color(color.r, color.g, color.b, 0f);
                Color midColor = new Color(color.r, color.g, color.b, sideAlpha);

                AddVertex(vertexHelper, inner, edgeColor);
                AddVertex(vertexHelper, outer, midColor);

                if (i < segments)
                {
                    int baseIndex = i * 2;
                    vertexHelper.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
                    vertexHelper.AddTriangle(baseIndex + 1, baseIndex + 3, baseIndex + 2);
                }
            }
        }

        private void AddVertex(VertexHelper vertexHelper, Vector2 position, Color vertexColor)
        {
            UIVertex vertex = new UIVertex();
            vertex.position = position;
            vertex.color = vertexColor;
            vertex.uv0 = Vector2.zero;
            vertexHelper.AddVert(vertex);
        }

        #endregion
    }
}
