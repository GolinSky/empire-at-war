using System;

namespace EmpireAtWar.Entities.TacticalMarkers
{
    public sealed class MarkerRadialModel
    {
        private const float HUB_RADIUS = 28f;
        private float _originX;
        private float _originY;
        private bool _hasMoved;
        public bool IsOpen { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        public event Action Changed;

        public void Open(float pointerX = 0f, float pointerY = 0f)
        {
            _originX = pointerX; _originY = pointerY; _hasMoved = false;
            IsOpen = true; SelectedIndex = -1; Changed?.Invoke();
        }
        public void Close() { IsOpen = false; SelectedIndex = -1; Changed?.Invoke(); }

        public void Select(float x, float y)
        {
            if (!_hasMoved)
            {
                float deltaX = x - _originX, deltaY = y - _originY;
                if (deltaX * deltaX + deltaY * deltaY < HUB_RADIUS * HUB_RADIUS) return;
                _hasMoved = true;
            }
            int selected = x * x + y * y < HUB_RADIUS * HUB_RADIUS ? -1 :
                (int)Math.Floor((Math.Atan2(x, y) * 180d / Math.PI + 390d) % 360d / 60d);
            if (selected == SelectedIndex) return;
            SelectedIndex = selected;
            Changed?.Invoke();
        }
    }
}
