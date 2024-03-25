using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MiniMap
{
    public class MarkView:MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private RectTransform rectTransform;

        public void SetData(Transform parent, Vector2 anchoredPosition, Sprite sprite)
        {
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = anchoredPosition;
            iconImage.sprite = sprite;
        }
    }
}