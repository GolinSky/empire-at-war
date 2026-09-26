using MPUIKIT;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>One screen-space hardpoint marker: dark square, symbol, frame, target halo and local HP strip.</summary>
    public sealed class HardPointMarkerView : MonoBehaviour
    {
        private const float ICON_BOX_SIZE = 28f;
        private const float ICON_SIZE = 20f;
        private const float HALO_SIZE = 36f;
        private const float HEALTH_WIDTH = 32f;
        private const float HEALTH_HEIGHT = 4f;
        private const float HEALTH_GAP = 3f;
        private const float HOVER_SCALE = 1.12f;
        private const float FRAME_WIDTH = 1.5f;
        private const float TARGET_FRAME_WIDTH = 2f;
        private const float HEALTHY_THRESHOLD = 0.6f;
        private const float CRITICAL_THRESHOLD = 0.3f;

        private static readonly Color BACKGROUND_COLOR = new(0.03f, 0.05f, 0.08f, 0.8f);
        private static readonly Color DESTROYED_BACKGROUND_COLOR = new(0.03f, 0.03f, 0.04f, 0.6f);
        private static readonly Color FRAME_COLOR = new(0.58f, 0.64f, 0.72f, 0.8f);
        private static readonly Color HOVER_FRAME_COLOR = new(0.22f, 0.74f, 0.97f, 1f);
        private static readonly Color TARGET_COLOR = new(0.98f, 0.45f, 0.09f, 1f);
        private static readonly Color HALO_COLOR = new(0.98f, 0.45f, 0.09f, 0.35f);
        private static readonly Color ICON_COLOR = new(0.97f, 0.98f, 0.99f, 1f);
        private static readonly Color DESTROYED_ICON_COLOR = new(0.5f, 0.52f, 0.56f, 0.45f);
        private static readonly Color TRACK_COLOR = new(0f, 0f, 0f, 0.9f);
        private static readonly Color HEALTHY_COLOR = new(0.25f, 0.9f, 0.42f, 1f);
        private static readonly Color DAMAGED_COLOR = new(0.96f, 0.7f, 0.2f, 1f);
        private static readonly Color CRITICAL_COLOR = new(0.94f, 0.27f, 0.27f, 1f);

        private RectTransform _root;
        private MPImage _halo;
        private MPImage _background;
        private Image _icon;
        private MPImage _healthFill;

        public static HardPointMarkerView Create(RectTransform parent)
        {
            GameObject markerObject = new("HardPointMarker", typeof(RectTransform));
            markerObject.layer = parent.gameObject.layer;
            RectTransform root = (RectTransform)markerObject.transform;
            root.SetParent(parent, false);
            root.sizeDelta = new Vector2(ICON_BOX_SIZE, ICON_BOX_SIZE);

            HardPointMarkerView marker = markerObject.AddComponent<HardPointMarkerView>();
            marker.Build(root);
            return marker;
        }

        public void Show(Vector2 localPosition, HardPointMarkerData data)
        {
            _root.anchoredPosition = localPosition;
            _root.localScale = Vector3.one * (data.IsHovered && !data.IsDestroyed ? HOVER_SCALE : 1f);

            _halo.enabled = data.IsTargeted;
            _background.color = data.IsDestroyed ? DESTROYED_BACKGROUND_COLOR : BACKGROUND_COLOR;
            _background.OutlineColor = data.IsTargeted ? TARGET_COLOR : data.IsHovered ? HOVER_FRAME_COLOR : FRAME_COLOR;
            _background.OutlineWidth = data.IsTargeted ? TARGET_FRAME_WIDTH : FRAME_WIDTH;

            _icon.sprite = data.Icon;
            _icon.color = data.IsDestroyed ? DESTROYED_ICON_COLOR : ICON_COLOR;

            float health = Mathf.Clamp01(data.HealthPercentage);
            _healthFill.fillAmount = health;
            _healthFill.color = health > HEALTHY_THRESHOLD ? HEALTHY_COLOR
                : health > CRITICAL_THRESHOLD ? DAMAGED_COLOR
                : CRITICAL_COLOR;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void Build(RectTransform root)
        {
            _root = root;
            _halo = HardPointOverlayGraphics.CreateImage("TargetHalo", root, new Vector2(HALO_SIZE, HALO_SIZE),
                HALO_COLOR, 6f);
            _background = HardPointOverlayGraphics.CreateImage("Background", root,
                new Vector2(ICON_BOX_SIZE, ICON_BOX_SIZE), BACKGROUND_COLOR, 4f);

            GameObject iconObject = new("Icon", typeof(RectTransform));
            iconObject.layer = root.gameObject.layer;
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.SetParent(root, false);
            iconRect.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);
            _icon = iconObject.AddComponent<Image>();
            _icon.raycastTarget = false;
            _icon.preserveAspect = true;

            float healthY = -(ICON_BOX_SIZE * 0.5f + HEALTH_GAP + HEALTH_HEIGHT * 0.5f);
            MPImage track = HardPointOverlayGraphics.CreateImage("HealthTrack", root,
                new Vector2(HEALTH_WIDTH, HEALTH_HEIGHT), TRACK_COLOR, 1f);
            track.rectTransform.anchoredPosition = new Vector2(0f, healthY);
            _healthFill = HardPointOverlayGraphics.CreateImage("HealthFill", track.rectTransform,
                new Vector2(HEALTH_WIDTH, HEALTH_HEIGHT), HEALTHY_COLOR, 1f);
            _healthFill.type = Image.Type.Filled;
            _healthFill.fillMethod = Image.FillMethod.Horizontal;
            _healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
}
