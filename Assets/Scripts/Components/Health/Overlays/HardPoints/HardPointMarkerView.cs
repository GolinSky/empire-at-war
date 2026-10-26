using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>One screen-space hardpoint icon tinted by its remaining health.</summary>
    public sealed class HardPointMarkerView : MonoBehaviour
    {
        private const float ICON_BOX_SIZE = 28f;
        private const float ICON_SIZE = 25f;
        private const float HOVER_SCALE = 1.12f;
        private const float HEALTH_HUE_RANGE = 1f / 3f;
        private const float HEALTH_SATURATION = 0.55f;
        private const float HEALTH_BRIGHTNESS = 0.75f;

        private RectTransform _root;
        private Image _icon;

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
            if (data.IsDestroyed)
            {
                Hide();
                return;
            }

            _root.anchoredPosition = localPosition;
            _root.localScale = Vector3.one * (data.Scale * (data.IsHovered ? HOVER_SCALE : 1f));

            _icon.sprite = data.Icon;
            _icon.color = Color.HSVToRGB(Mathf.Clamp01(data.HealthPercentage) * HEALTH_HUE_RANGE,
                HEALTH_SATURATION, HEALTH_BRIGHTNESS);

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

            GameObject iconObject = new("Icon", typeof(RectTransform));
            iconObject.layer = root.gameObject.layer;
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.SetParent(root, false);
            iconRect.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);
            _icon = iconObject.AddComponent<HardPointIconImage>();
            _icon.raycastTarget = false;
            _icon.preserveAspect = true;

            Outline outline = iconObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.03f, 0.04f, 0.06f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }
    }
}
