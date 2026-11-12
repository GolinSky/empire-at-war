using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Registered IMGUI overlays and EventSystem graphics that receive pointer events block world input.
    /// </summary>
    public sealed class UiHitTest : IUiHitTest
    {
        private readonly List<RaycastResult> _results = new List<RaycastResult>();
        private readonly HashSet<Func<Vector2, bool>> _overlays = new HashSet<Func<Vector2, bool>>();
        private EventSystem _eventSystem;
        private PointerEventData _eventData;

        public void RegisterOverlay(Func<Vector2, bool> containsPointer) => _overlays.Add(containsPointer);

        public void UnregisterOverlay(Func<Vector2, bool> containsPointer) => _overlays.Remove(containsPointer);

        public bool IsOverUi(Vector2 screenPosition)
        {
            foreach (Func<Vector2, bool> containsPointer in _overlays)
                if (containsPointer(screenPosition)) return true;

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
                if (result.module is GraphicRaycaster) { hit = true; break; }
            _results.Clear();
            return hit;
        }
    }
}
