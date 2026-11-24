using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>One screen-space hardpoint icon tinted by its remaining health; fades in on show and out on hide.</summary>
    public sealed class HardPointMarkerView : MonoBehaviour
    {
        private const float ICON_BOX_SIZE = 28f;
        private const float ICON_SIZE = 25f;
        private const float HEALTH_HUE_RANGE = 1f / 3f;
        private const float HEALTH_SATURATION = 0.55f;
        private const float HEALTH_BRIGHTNESS = 0.75f;
        private const float FADE_DURATION = 0.15f;

        private RectTransform _root;
        private Image _icon;
        private HardPointTargetRing _targetRing;
        private CanvasGroup _canvasGroup;
        private float _targetAlpha;

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
            _root.localScale = Vector3.one * data.DisplayScale;

            _icon.sprite = data.Icon;
            _icon.color = Color.HSVToRGB(Mathf.Clamp01(data.HealthPercentage) * HEALTH_HUE_RANGE,
                HEALTH_SATURATION, HEALTH_BRIGHTNESS);
            _targetRing.gameObject.SetActive(data.IsTargeted);
            if (data.IsTargeted) _root.SetAsLastSibling();

            _targetAlpha = 1f;
            enabled = _canvasGroup.alpha < 1f;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            _targetAlpha = 0f;
            if (_canvasGroup.alpha <= 0f)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }

            enabled = true;
        }

        private void Update()
        {
            Fade(Time.unscaledDeltaTime);
        }

        internal void Fade(float deltaTime)
        {
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, deltaTime / FADE_DURATION);
            if (_canvasGroup.alpha != _targetAlpha) return;

            enabled = false;
            if (_targetAlpha <= 0f) gameObject.SetActive(false);
        }

        private void Build(RectTransform root)
        {
            _root = root;
            _canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            enabled = false;

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

            GameObject ringObject = new("TargetRing", typeof(RectTransform));
            ringObject.layer = root.gameObject.layer;
            RectTransform ringRect = (RectTransform)ringObject.transform;
            ringRect.SetParent(root, false);
            ringRect.sizeDelta = new Vector2(44f, 44f);
            _targetRing = ringObject.AddComponent<HardPointTargetRing>();
            _targetRing.color = new Color32(210, 169, 94, 255);
            _targetRing.raycastTarget = false;
            ringObject.SetActive(false);
        }
    }
}
