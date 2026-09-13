using DG.Tweening;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiniMap;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MiniMap
{
    public class MarkView:MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private RectTransform rectTransform;
        
        private IMiniMapPositionConvector _miniMapPositionConvector;
        private IMarkData _markData;
        private MiniMapMarker _marker;
        
        public Image IconImage => iconImage;

        public void SetData(Transform parent, Vector2 position, Sprite sprite)
        {
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = position;
            iconImage.sprite = sprite;
        }
        
        public void SetData(IMiniMapPositionConvector miniMapPositionConvector, Transform parent, IMarkData markData)
        {
            _miniMapPositionConvector = miniMapPositionConvector;
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = miniMapPositionConvector.GetPosition(markData.Position);
            iconImage.sprite = markData.Icon;
            _markData = markData;
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
            if (marker.WorldDiameter > 0f)
            {
                rectTransform.SetAsFirstSibling();
            }

            RefreshMarker();
        }

        public void Release()
        {
            iconImage.DOKill();
        }

        private void Update()
        {
            if (_markData != null)
            {
                rectTransform.anchoredPosition = _miniMapPositionConvector.GetPosition(_markData.Position);
            }

            if (_marker != null)
            {
                RefreshMarker();
            }
        }

        private void RefreshMarker()
        {
            rectTransform.anchoredPosition = _miniMapPositionConvector.GetPosition(
                new Vector3(_marker.X, 0f, _marker.Z));
            if (_marker.WorldDiameter > 0f)
            {
                rectTransform.sizeDelta = _miniMapPositionConvector.GetSize(_marker.WorldDiameter);
            }

            Color color = _marker.Relation switch
            {
                PlayerType.Player => new Color(0.15f, 0.65f, 1f),
                PlayerType.Opponent => new Color(1f, 0.2f, 0.15f),
                _ => new Color(0.7f, 0.7f, 0.7f),
            };
            color.a = iconImage.color.a;
            iconImage.color = color;
            iconImage.enabled = _marker.Visible;
        }
    }
}
