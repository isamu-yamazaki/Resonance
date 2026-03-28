using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.UI
{
    /// <summary>
    /// Singleton that renders a screen-space interact prompt at the bottom center of the HUD.
    /// Call Show(keyLabel, actionLabel) to display, Hide() to dismiss.
    /// Built entirely in code — no prefab required.
    /// </summary>
    public class InteractPromptUI : MonoBehaviour
    {
        public static InteractPromptUI Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, 80f);

        // Style
        private const float KeyBoxSize = 48f;
        private const float FontSizeKey = 22f;
        private const float FontSizeLabel = 18f;
        private const float Spacing = 12f;
        private const float PaddingH = 24f;
        private const float PaddingV = 14f;

        private static readonly Color ColorBackground = new Color(0.08f, 0.08f, 0.10f, 0.55f);
        private static readonly Color ColorKeyBox = new Color(1f, 1f, 1f, 1f);
        private static readonly Color ColorKeyText = new Color(0.05f, 0.05f, 0.05f, 1f);
        private static readonly Color ColorLabelText = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color ColorAccent = new Color(0.35f, 0.85f, 1f, 1f);

        private Canvas _canvas;
        private TextMeshProUGUI _keyText;
        private TextMeshProUGUI _labelText;
        private bool _visible;

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Public API

        public void Show(string keyLabel, string actionLabel)
        {
            _keyText.text = keyLabel.ToUpper();
            _labelText.text = actionLabel.ToUpper();
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        #endregion

        #region UI Construction

        private void BuildUI()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;

            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            // Background panel anchored to bottom center
            GameObject panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(transform, false);

            RectTransform panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = anchoredPosition;

            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = ColorBackground;

            HorizontalLayoutGroup layout = panelGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = Spacing;
            layout.padding = new RectOffset((int)PaddingH, (int)PaddingH, (int)PaddingV, (int)PaddingV);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panelGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Key box
            GameObject keyBoxGO = new GameObject("KeyBox");
            keyBoxGO.transform.SetParent(panelGO.transform, false);

            keyBoxGO.AddComponent<RectTransform>().sizeDelta = new Vector2(KeyBoxSize, KeyBoxSize);

            Image keyBoxImage = keyBoxGO.AddComponent<Image>();
            keyBoxImage.color = ColorKeyBox;

            LayoutElement keyBoxLayout = keyBoxGO.AddComponent<LayoutElement>();
            keyBoxLayout.preferredWidth = KeyBoxSize;
            keyBoxLayout.preferredHeight = KeyBoxSize;
            keyBoxLayout.minWidth = KeyBoxSize;
            keyBoxLayout.minHeight = KeyBoxSize;

            // Key letter
            GameObject keyTextGO = new GameObject("KeyText");
            keyTextGO.transform.SetParent(keyBoxGO.transform, false);

            RectTransform keyTextRect = keyTextGO.AddComponent<RectTransform>();
            keyTextRect.anchorMin = Vector2.zero;
            keyTextRect.anchorMax = Vector2.one;
            keyTextRect.sizeDelta = Vector2.zero;

            _keyText = keyTextGO.AddComponent<TextMeshProUGUI>();
            _keyText.text = "E";
            _keyText.fontSize = FontSizeKey;
            _keyText.fontStyle = FontStyles.Bold;
            _keyText.color = ColorKeyText;
            _keyText.alignment = TextAlignmentOptions.Center;
            if (fontAsset != null) _keyText.font = fontAsset;

            // Divider
            GameObject dividerGO = new GameObject("Divider");
            dividerGO.transform.SetParent(panelGO.transform, false);

            dividerGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            LayoutElement dividerLayout = dividerGO.AddComponent<LayoutElement>();
            dividerLayout.preferredWidth = 1f;
            dividerLayout.preferredHeight = 28f;
            dividerLayout.minWidth = 1f;

            // Action label
            GameObject labelGO = new GameObject("ActionLabel");
            labelGO.transform.SetParent(panelGO.transform, false);

            _labelText = labelGO.AddComponent<TextMeshProUGUI>();
            _labelText.text = "RIDE";
            _labelText.fontSize = FontSizeLabel;
            _labelText.fontStyle = FontStyles.Bold;
            _labelText.color = ColorLabelText;
            _labelText.alignment = TextAlignmentOptions.MidlineLeft;
            if (fontAsset != null) _labelText.font = fontAsset;

            labelGO.AddComponent<LayoutElement>().preferredWidth = 120f;

            // Accent line at bottom of panel
            GameObject accentGO = new GameObject("AccentLine");
            accentGO.transform.SetParent(panelGO.transform, false);

            RectTransform accentRect = accentGO.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(1f, 0f);
            accentRect.pivot = new Vector2(0.5f, 0f);
            accentRect.sizeDelta = new Vector2(0f, 2f);
            accentRect.anchoredPosition = Vector2.zero;

            accentGO.AddComponent<Image>().color = ColorAccent;
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            if (_canvas != null)
                _canvas.gameObject.SetActive(visible);
        }

        #endregion
    }
}
