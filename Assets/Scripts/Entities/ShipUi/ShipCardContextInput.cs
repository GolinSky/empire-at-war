using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EmpireAtWar.Entities.ShipUi
{
    public sealed class ShipCardContextInput : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        private long _targetId;
        private string _displayName;
        private Action<long, string, Vector2> _open;
        private bool _rightPressed;

        public void Bind(long targetId, string displayName, Action<long, string, Vector2> open)
        {
            _targetId = targetId;
            _displayName = displayName;
            _open = open;
            _rightPressed = false;
        }

        public void Clear() => Bind(default, null, null);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                _rightPressed = _open != null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || !_rightPressed)
                return;

            _rightPressed = false;
            eventData.Use();
            _open(_targetId, _displayName, eventData.position);
        }
    }
}
