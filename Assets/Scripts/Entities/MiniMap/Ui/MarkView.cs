using DG.Tweening;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.MiniMap;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MiniMap
{
    public class MarkView:MonoBehaviour
    {
        private const float STATION_SIZE = 18f;
        private const float SHIP_SIZE = 6f;
        private const float PLATFORM_SIZE = 14f;
        private const float ZONE_SIZE_SCALE = 0.65f;
        private const float ZONE_ALPHA = 0.35f;

        [SerializeField] private Image iconImage;
        [SerializeField] private RectTransform rectTransform;
        
        private IMiniMapPositionConvector _miniMapPositionConvector;
        private MiniMapMarker _marker;
        
        public Image IconImage => iconImage;

        public void SetData(Transform parent, Vector2 position, Sprite sprite)
        {
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = Vector2.one * STATION_SIZE;
            iconImage.sprite = sprite;
        }
        
        public void SetData(
            IMiniMapPositionConvector miniMapPositionConvector,
            Transform parent,
            MiniMapMarker marker,
            Sprite sprite)
        {
            _miniMapPositionConvector = miniMapPositionConvector;
            rectTransform.SetParent(parent, false);
            iconImage.sprite = sprite;
            iconImage.raycastTarget = false;
            _marker = marker;
            rectTransform.sizeDelta = Vector2.one * (marker.MarkType == MarkType.Ship
                ? SHIP_SIZE
                : PLATFORM_SIZE);
            if (marker.WorldDiameter > 0f)
            {
                rectTransform.SetAsFirstSibling();
                // Preserve zone translucency while minimap hover fades animate the image color.
                iconImage.canvasRenderer.SetAlpha(ZONE_ALPHA);
            }

            RefreshMarker();
        }

        public void Release()
        {
            iconImage.DOKill();
        }

        private void Update()
        {
            if (_marker != null)
            {
                RefreshMarker();
            }
        }

        public static Color GetRelationColor(OwnerRelation relation)
        {
            return relation switch
            {
                OwnerRelation.Own => new Color(0.15f, 0.65f, 1f),
                OwnerRelation.Ally => new Color(0.3f, 0.9f, 0.5f),
                OwnerRelation.Enemy => new Color(1f, 0.2f, 0.15f),
                _ => new Color(0.7f, 0.7f, 0.7f),
            };
        }

        private void RefreshMarker()
        {
            rectTransform.anchoredPosition = _miniMapPositionConvector.GetPosition(
                new Vector3(_marker.X, 0f, _marker.Z));
            if (_marker.WorldDiameter > 0f)
            {
                rectTransform.sizeDelta = _miniMapPositionConvector.GetSize(_marker.WorldDiameter) * ZONE_SIZE_SCALE;
            }

            Color color = GetRelationColor(_marker.Relation);
            color.a = iconImage.color.a;
            iconImage.color = color;
            iconImage.enabled = _marker.Visible;
        }
    }
}
