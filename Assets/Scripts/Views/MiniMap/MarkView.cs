using System;
using EmpireAtWar.Models.MiniMap;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MiniMap
{
    public class MarkView:MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private RectTransform rectTransform;
        
        private IMiniMapPositionConvector miniMapPositionConvector;
        private IMarkData markData;
        
        public Image IconImage => iconImage;

        public void SetData(Transform parent, Vector2 position, Sprite sprite)
        {
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = position;
            iconImage.sprite = sprite;
        }
        
        public void SetData(IMiniMapPositionConvector miniMapPositionConvector, Transform parent, IMarkData markData)
        {
            this.miniMapPositionConvector = miniMapPositionConvector;
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = miniMapPositionConvector.GetPosition(markData.Position);
            iconImage.sprite = markData.Icon;
            this.markData = markData;
        }

        private void Update()
        {
            if (markData != null)
            {
                rectTransform.anchoredPosition = miniMapPositionConvector.GetPosition(markData.Position);
            }
        }
    }
}