using System;
using MPUIKIT;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ship.Health.Overlay
{
    public interface IHealthOverlayView
    {
        void SetValues(float armorPercentage, float shieldPercentage);
        void Show(Vector2 screenPosition);
        void Hide();
    }

    public sealed class HealthOverlayView : MonoBehaviour, IHealthOverlayView
    {
        private const float PANEL_WIDTH = 220f;
        private const float PANEL_HEIGHT = 38f;
        private const float BAR_WIDTH = 204f;
        private const float BAR_HEIGHT = 8f;
        private const float SCREEN_OFFSET = 44f;

        private static readonly Color PANEL_COLOR = new(0.025f, 0.035f, 0.055f, 0.92f);
        private static readonly Color PANEL_OUTLINE_COLOR = new(0.35f, 0.48f, 0.62f, 0.75f);
        private static readonly Color BAR_BACKGROUND_COLOR = new(0.08f, 0.1f, 0.14f, 0.96f);
        private static readonly Color ARMOR_COLOR = new(0.25f, 0.9f, 0.42f, 1f);
        private static readonly Color SHIELD_COLOR = new(0.2f, 0.72f, 1f, 1f);

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _panel;
        private MPImage _armorFill;
        private MPImage _shieldFill;

        private void Awake()
        {
            BuildCanvas();
            Hide();
        }

        public void SetValues(float armorPercentage, float shieldPercentage)
        {
            _armorFill.fillAmount = Mathf.Clamp01(armorPercentage);
            _shieldFill.fillAmount = Mathf.Clamp01(shieldPercentage);
        }

        public void Show(Vector2 screenPosition)
        {
            Vector2 offsetPosition = screenPosition + Vector2.up * SCREEN_OFFSET;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                offsetPosition,
                null,
                out Vector2 localPosition);

            Rect canvasBounds = _canvasRect.rect;
            float halfWidth = PANEL_WIDTH * 0.5f;
            float halfHeight = PANEL_HEIGHT * 0.5f;
            localPosition.x = Mathf.Clamp(
                localPosition.x,
                canvasBounds.xMin + halfWidth,
                canvasBounds.xMax - halfWidth);
            localPosition.y = Mathf.Clamp(
                localPosition.y,
                canvasBounds.yMin + halfHeight,
                canvasBounds.yMax - halfHeight);

            _panel.anchoredPosition = localPosition;
            if (!_panel.gameObject.activeSelf)
            {
                _panel.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_panel == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(HealthOverlayView)} was not initialized.");
            }

            _panel.gameObject.SetActive(false);
        }

        private void BuildCanvas()
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0)
            {
                throw new InvalidOperationException("The UI layer is required for the health overlay.");
            }

            GameObject canvasObject = new("HealthOverlayCanvas", typeof(RectTransform));
            canvasObject.layer = uiLayer;
            canvasObject.transform.SetParent(transform, false);

            _canvasRect = (RectTransform)canvasObject.transform;
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;

            MPImage panelImage = CreateImage(
                "UnitHealthOverlay",
                _canvasRect,
                new Vector2(PANEL_WIDTH, PANEL_HEIGHT),
                PANEL_COLOR,
                7f);
            panelImage.OutlineWidth = 1f;
            panelImage.OutlineColor = PANEL_OUTLINE_COLOR;
            _panel = panelImage.rectTransform;

            CreateBar("Shield", 6f, SHIELD_COLOR, out _shieldFill);
            CreateBar("Armor", -6f, ARMOR_COLOR, out _armorFill);
        }

        private void CreateBar(string name, float y, Color fillColor, out MPImage fill)
        {
            MPImage background = CreateImage(
                $"{name}Background",
                _panel,
                new Vector2(BAR_WIDTH, BAR_HEIGHT),
                BAR_BACKGROUND_COLOR,
                4f);
            background.rectTransform.anchoredPosition = new Vector2(0f, y);

            fill = CreateImage(
                $"{name}Fill",
                background.rectTransform,
                new Vector2(BAR_WIDTH, BAR_HEIGHT),
                fillColor,
                4f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillClockwise = true;
            fill.fillAmount = 1f;
        }

        private static MPImage CreateImage(
            string name,
            RectTransform parent,
            Vector2 size,
            Color color,
            float cornerRadius)
        {
            GameObject imageObject = new(name, typeof(RectTransform));
            imageObject.layer = parent.gameObject.layer;
            RectTransform rectTransform = (RectTransform)imageObject.transform;
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = Vector2.zero;

            MPImage image = imageObject.AddComponent<MPImage>();
            image.raycastTarget = false;
            image.color = color;
            image.DrawShape = DrawShape.Rectangle;
            image.FalloffDistance = 1f;

            Rectangle rectangle = image.Rectangle;
            rectangle.CornerRadius = Vector4.one * cornerRadius;
            image.Rectangle = rectangle;
            return image;
        }
    }
}
