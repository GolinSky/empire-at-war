using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Popups
{
    public sealed class PlanetSelectionScrollRect : ScrollRect
    {
        private const float WHEEL_EASING_RATE = 22f;
        private const float WHEEL_SETTLE_DISTANCE = 0.1f;

        private float? _wheelTarget;
        private bool _isDragging;

        private float ScrollableHeight => content.rect.height - viewport.rect.height;

        public override void OnScroll(PointerEventData eventData)
        {
            if (!IsActive() || _isDragging) return;

            float scrollableHeight = ScrollableHeight;
            if (scrollableHeight <= 0f) return;

            Vector2 delta = eventData.scrollDelta;
            float wheelDelta = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? -delta.x : delta.y;
            if (wheelDelta == 0f) return;

            float current = verticalNormalizedPosition;
            float target = _wheelTarget ?? current;
            if ((target - current) * wheelDelta < 0f) target = current;

            base.StopMovement();
            _wheelTarget = Mathf.Clamp01(target + wheelDelta * scrollSensitivity / scrollableHeight);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsActive())
            {
                _isDragging = true;
                _wheelTarget = null;
            }
            base.OnBeginDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _isDragging = false;
            base.OnEndDrag(eventData);
        }

        public override void StopMovement()
        {
            _wheelTarget = null;
            base.StopMovement();
        }

        protected override void SetNormalizedPosition(float value, int axis)
        {
            if (axis == 1) _wheelTarget = null;
            base.SetNormalizedPosition(value, axis);
        }

        protected override void OnDisable()
        {
            _isDragging = false;
            _wheelTarget = null;
            base.OnDisable();
        }

        protected override void LateUpdate()
        {
            if (_wheelTarget is float target && Time.unscaledDeltaTime > 0f)
            {
                float remaining = (verticalNormalizedPosition - target) * ScrollableHeight;
                float step = remaining * (1f - Mathf.Exp(-WHEEL_EASING_RATE * Time.unscaledDeltaTime));
                if (Mathf.Abs(remaining - step) <= WHEEL_SETTLE_DISTANCE)
                {
                    step = remaining;
                    _wheelTarget = null;
                }
                SetContentAnchoredPosition(content.anchoredPosition + new Vector2(0f, step));
            }
            base.LateUpdate();
        }
    }
}
