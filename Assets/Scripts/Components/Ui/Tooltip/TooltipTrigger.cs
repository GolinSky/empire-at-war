using System;
using EmpireAtWar.Services.Tooltip;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EmpireAtWar.Components.Ui.Tooltip
{
    public sealed class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform anchorRect;
        private object _key;

        [SerializeField] private string key;

        private TooltipAnchor _anchor;

        private bool _hovered;

        public event Action<object, TooltipAnchor, object> HoverStarted;

        public event Action<object> HoverEnded;

        public event Action<TooltipTrigger> Destroyed;

        public void SetKey(object value)
        {
            if (Equals(_key, value)) return;
            bool wasHovered = _hovered;
            EndHover();
            _key = value;
            if (wasHovered)
            {
                _hovered = true;
                HoverStarted?.Invoke(_key ?? key, _anchor, this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Vector3[] corners = new Vector3[4];
            anchorRect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventData.enterEventCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(eventData.enterEventCamera, corners[2]);
            _anchor = new TooltipAnchor(TooltipAnchorKind.UiRect, min.x, min.y, max.x - min.x, max.y - min.y);
            _hovered = true;
            HoverStarted?.Invoke(_key ?? key, _anchor, this);
        }

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        private void OnDisable() => EndHover();

        private void OnDestroy() { EndHover(); Destroyed?.Invoke(this); }

        private void EndHover()
        {
            if (!_hovered) return;
            _hovered = false;
            HoverEnded?.Invoke(this);
        }
    }
}
