using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EmpireAtWar.Services.InputService
{
    public sealed class UiHitTest : IUiHitTest
    {
        private readonly int _uiLayer = LayerMask.NameToLayer("UI");
        private readonly List<RaycastResult> _results = new List<RaycastResult>();
        private EventSystem _eventSystem;
        private PointerEventData _eventData;

        public bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem current = EventSystem.current;
            if (current == null) return false;
            if (_eventSystem != current)
            {
                _eventSystem = current;
                _eventData = new PointerEventData(current);
            }
            _eventData.Reset();
            _eventData.position = screenPosition;
            _results.Clear();
            current.RaycastAll(_eventData, _results);
            bool hit = false;
            foreach (RaycastResult result in _results)
                if (result.gameObject.layer == _uiLayer) { hit = true; break; }
            _results.Clear();
            return hit;
        }
    }
}
