using EmpireAtWar.Components.Ui.Tooltip;
using DG.Tweening;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.TacticalMarkers;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MiniMap
{
    public class MarkView:MonoBehaviour
    {
        private const float STATION_SIZE = 24f;
        private const float SHIP_SIZE = 6f;
        private const float PLATFORM_SIZE = 14f;
        private const float TIBANNA_GAS_STATION_SIZE = 10f;
        private const float ZONE_SIZE_SCALE = 0.65f;
        private const float ZONE_ALPHA = 0.35f;

        private IMiniMapPositionConvector _miniMapPositionConvector;
        private IPlayerColors _playerColors;

        [SerializeField] private Image iconImage;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TooltipTrigger tooltipTrigger;
        [SerializeField] private Image arrivalRing;
        [SerializeField] private TacticalMarkerSettings tacticalMarkerSettings;
        private MiniMapMarker _marker;

        public TooltipTrigger TooltipTrigger => tooltipTrigger;

        public Image IconImage => iconImage;

        public void SetData(Transform parent, Vector2 position, Sprite sprite)
        {
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = Vector2.one * STATION_SIZE;
            iconImage.sprite = sprite;
            tooltipTrigger.SetKey("Station");
        }

        public void SetData(
            IMiniMapPositionConvector miniMapPositionConvector,
            IPlayerColors playerColors,
            Transform parent,
            MiniMapMarker marker,
            Sprite sprite)
        {
            _miniMapPositionConvector = miniMapPositionConvector;
            _playerColors = playerColors;
            rectTransform.SetParent(parent, false);
            iconImage.sprite = sprite;
            iconImage.raycastTarget = true;
            _marker = marker;
            _marker.Changed += Refresh;
            tooltipTrigger.SetKey(marker);
            rectTransform.sizeDelta = Vector2.one * (marker.MarkType switch
            {
                MarkType.Ship => SHIP_SIZE,
                MarkType.PlayerBase or MarkType.EnemyBase => STATION_SIZE,
                MarkType.TacticalLook or MarkType.TacticalAttack or MarkType.TacticalCapture or
                    MarkType.TacticalDefend or MarkType.TacticalRally or MarkType.TacticalRetreat => STATION_SIZE,
                MarkType.TibannaGasStation => TIBANNA_GAS_STATION_SIZE,
                _ => PLATFORM_SIZE
            });
            if (marker.WorldDiameter > 0f)
            {
                rectTransform.SetAsFirstSibling();
                // Preserve zone translucency while minimap hover fades animate the image color.
                iconImage.canvasRenderer.SetAlpha(ZONE_ALPHA);
            }

            Refresh();
            if (marker.MarkType >= MarkType.TacticalLook && marker.MarkType <= MarkType.TacticalRetreat)
            {
                arrivalRing.gameObject.SetActive(true);
                arrivalRing.color = tacticalMarkerSettings.GetColor(
                    (TacticalMarkerType)((int)marker.MarkType - (int)MarkType.TacticalLook));
                arrivalRing.rectTransform.localScale = Vector3.one;
                arrivalRing.rectTransform.DOScale(2.4f, 0.6f).SetLink(gameObject);
                arrivalRing.DOFade(0f, 0.6f).SetLink(gameObject).OnComplete(() => arrivalRing.gameObject.SetActive(false));
            }
        }

        public void Release()
        {
            if (iconImage != null) iconImage.DOKill();
            if (arrivalRing != null)
            {
                arrivalRing.DOKill();
                arrivalRing.rectTransform.DOKill();
                arrivalRing.gameObject.SetActive(false);
            }
            if (_marker != null) _marker.Changed -= Refresh;
        }

        // Also covers scene teardown, which can destroy the view before its marker is removed.
        private void OnDestroy()
        {
            if (_marker != null)
            {
                _marker.Changed -= Refresh;
            }
        }

        public void Refresh()
        {
            rectTransform.anchoredPosition = _miniMapPositionConvector.GetPosition(
                new Vector3(_marker.X, 0f, _marker.Z));
            if (_marker.WorldDiameter > 0f)
            {
                rectTransform.sizeDelta = _miniMapPositionConvector.GetSize(_marker.WorldDiameter) * ZONE_SIZE_SCALE;
            }

            Color color = _marker.MarkType >= MarkType.TacticalLook && _marker.MarkType <= MarkType.TacticalRetreat
                ? Color.white : _playerColors.GetColor(_marker.Owner);
            color.a = iconImage.color.a;
            iconImage.color = color;
            iconImage.enabled = _marker.Visible;
        }
    }
}
