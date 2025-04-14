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

        private void Update()
        {
            if (_markData != null)
            {
                rectTransform.anchoredPosition = _miniMapPositionConvector.GetPosition(_markData.Position);
            }
        }
    }
}