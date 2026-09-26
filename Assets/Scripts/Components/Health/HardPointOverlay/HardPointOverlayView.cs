using System.Collections.Generic;
using MPUIKIT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    public sealed class HardPointOverlayView : MonoBehaviour, IHardPointOverlayView
    {
        private const float TOOLTIP_WIDTH = 300f;
        private const float TOOLTIP_OFFSET = 26f;
        private const int TOOLTIP_PADDING = 10;
        private const float TOOLTIP_SPACING = 4f;
        private const float TITLE_FONT_SIZE = 17f;
        private const float BODY_FONT_SIZE = 14f;

        private static readonly Color TOOLTIP_COLOR = new(0.03f, 0.05f, 0.08f, 0.96f);
        private static readonly Color TOOLTIP_OUTLINE_COLOR = new(0.22f, 0.74f, 0.97f, 0.9f);
        private static readonly Color TITLE_COLOR = new(0.97f, 0.98f, 0.99f, 1f);
        private static readonly Color BODY_COLOR = new(0.8f, 0.84f, 0.88f, 1f);

        private readonly List<HardPointMarkerView> _markers = new List<HardPointMarkerView>();
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _markerRoot;
        private RectTransform _tooltip;
        private TextMeshProUGUI _tooltipTitle;
        private TextMeshProUGUI _tooltipBody;

        public float ScaleFactor => _canvas.scaleFactor;

        private void Awake()
        {
            BuildCanvas();
            HideTooltip();
        }

        public void ShowMarker(int slot, HardPointMarkerData data)
        {
            while (_markers.Count <= slot)
            {
                _markers.Add(HardPointMarkerView.Create(_markerRoot));
            }

            _markers[slot].Show(ToLocal(data.ScreenPosition), data);
        }

        public void HideMarkersFrom(int slot)
        {
            for (int index = slot; index < _markers.Count; index++)
            {
                _markers[index].Hide();
            }
        }

        public void ShowTooltip(Vector2 anchorScreenPosition, string title, string body)
        {
            _tooltipTitle.text = title;
            _tooltipBody.text = body;
            if (!_tooltip.gameObject.activeSelf)
            {
                _tooltip.gameObject.SetActive(true);
            }

            // Sit beside the marker, away from the cursor, and flip to the left near the right screen edge.
            Vector2 anchor = ToLocal(anchorScreenPosition);
            Rect bounds = _canvasRect.rect;
            bool placeLeft = anchor.x + TOOLTIP_OFFSET + TOOLTIP_WIDTH > bounds.xMax;
            _tooltip.pivot = new Vector2(placeLeft ? 1f : 0f, 0.5f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltip);

            float halfHeight = _tooltip.rect.height * 0.5f;
            Vector2 position = anchor + Vector2.right * (placeLeft ? -TOOLTIP_OFFSET : TOOLTIP_OFFSET);
            position.y = Mathf.Clamp(position.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight);
            _tooltip.anchoredPosition = position;
        }

        public void HideTooltip()
        {
            _tooltip.gameObject.SetActive(false);
        }

        private Vector2 ToLocal(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, null,
                out Vector2 localPosition);
            return localPosition;
        }

        private void BuildCanvas()
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            GameObject canvasObject = new("HardPointOverlayCanvas", typeof(RectTransform));
            canvasObject.layer = uiLayer;
            canvasObject.transform.SetParent(transform, false);

            _canvasRect = (RectTransform)canvasObject.transform;
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = -1;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject markerRootObject = new("Markers", typeof(RectTransform));
            markerRootObject.layer = uiLayer;
            _markerRoot = (RectTransform)markerRootObject.transform;
            _markerRoot.SetParent(_canvasRect, false);

            BuildTooltip();
        }

        private void BuildTooltip()
        {
            MPImage panel = HardPointOverlayGraphics.CreateImage("HardPointTooltip", _canvasRect,
                new Vector2(TOOLTIP_WIDTH, 0f), TOOLTIP_COLOR, 10f);
            panel.OutlineWidth = 1.5f;
            panel.OutlineColor = TOOLTIP_OUTLINE_COLOR;
            _tooltip = panel.rectTransform;

            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(TOOLTIP_PADDING, TOOLTIP_PADDING, TOOLTIP_PADDING, TOOLTIP_PADDING);
            layout.spacing = TOOLTIP_SPACING;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _tooltipTitle = CreateText("Title", TITLE_FONT_SIZE, FontStyles.Bold, TITLE_COLOR);
            _tooltipBody = CreateText("Body", BODY_FONT_SIZE, FontStyles.Normal, BODY_COLOR);
        }

        private TextMeshProUGUI CreateText(string name, float fontSize, FontStyles style, Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform));
            textObject.layer = _tooltip.gameObject.layer;
            textObject.transform.SetParent(_tooltip, false);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
    }
}
