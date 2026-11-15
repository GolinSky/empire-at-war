using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.ShipUi
{
    public sealed class ShipDestroyMenuUi : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Button destroyButton;

        public event Action DestroyPressed;
        public event Action Dismissed;

        public void Initialize() => destroyButton.onClick.AddListener(PressDestroy);

        public void Dispose() => destroyButton.onClick.RemoveListener(PressDestroy);

        public void Show(Vector2 screenPosition)
        {
            var surface = (RectTransform)transform;
            var action = (RectTransform)destroyButton.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, screenPosition, null, out Vector2 point);
            Rect bounds = surface.rect;
            Rect size = action.rect;
            action.anchoredPosition = new Vector2(
                Mathf.Clamp(point.x, bounds.xMin - size.xMin, Mathf.Max(bounds.xMin - size.xMin, bounds.xMax - size.xMax)),
                Mathf.Clamp(point.y, bounds.yMin - size.yMin, Mathf.Max(bounds.yMin - size.yMin, bounds.yMax - size.yMax)));
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide() => gameObject.SetActive(false);

        private void OnDisable()
        {
            if (gameObject.activeSelf)
                Dismissed?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();
            Dismissed?.Invoke();
        }

        private void PressDestroy() => DestroyPressed?.Invoke();
    }
}
