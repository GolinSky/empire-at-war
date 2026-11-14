using System;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Models.Players;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    public sealed class BattleMarker : IBattleMarker, IDisposable
    {
        private const float HIT_SETTLE_PIXELS = 0.5f;
        private readonly IBattleMarkerLayer _layer;
        private readonly RectTransform _root;
        private readonly Image _frame;
        private readonly Image _icon;
        private readonly float _visualSize;
        private bool _registered;
        private float _zoom;
        private float _hitSize;
        private Vector2 _layoutOffset;
        private Vector2 _lastLayout;
        private long _lastGroup;
        private Vector2Int _lastCell;

        public long EntityId { get; private set; }
        public bool IsSquadron { get; }
        public bool IsShown { get; private set; }
        public Rect ScreenRect { get; private set; }
        public int DrawOrder { get; internal set; }

        internal readonly float ClickSize;
        internal readonly float ScreenOffset;
        internal Transform AnchorTransform { get; set; }
        internal Vector3 Position => IsSquadron ? _position : AnchorTransform.position;
        internal Vector3 Velocity { get; set; }
        internal PlayerId Owner { get; private set; }
        internal bool IsVisible { get; private set; }
        internal Action SampleVisibility { get; set; }

        private Vector3 _position;

        public BattleMarker(IBattleMarkerLayer layer, RectTransform root, Image frame, Image icon,
            float visualSize = 72f, float clickSize = 132f, float screenOffset = 60f, bool isSquadron = false)
        {
            _layer = layer;
            _root = root;
            _frame = frame;
            _icon = icon;
            _visualSize = visualSize;
            ClickSize = clickSize;
            ScreenOffset = screenOffset;
            IsSquadron = isSquadron;
        }

        public void Register(long entityId, PlayerId owner)
        {
            EntityId = entityId;
            Owner = owner;
            _layer.Attach(_root);
            _root.sizeDelta = Vector2.one * _visualSize;
            _root.gameObject.SetActive(false);
            _zoom = 0f;
            _registered = true;
            _layer.Register(this);
        }

        public void Unregister()
        {
            if (!_registered) return;
            _registered = false;
            SetVisible(false);
            _layer.Unregister(this);
        }

        public void Dispose()
        {
            Unregister();
            if (_root == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(_root.gameObject);
            else UnityEngine.Object.DestroyImmediate(_root.gameObject);
        }

        public void SetPosition(Vector3 position) => _position = position;

        internal void ResetPosition(Vector3 position)
        {
            _position = position;
            Velocity = Vector3.zero;
            if (_registered) _layer.InvalidateMembership(EntityId);
            Hide();
        }

        public void SetVisible(bool visible)
        {
            if (IsVisible == visible) return;
            IsVisible = visible;
            if (!visible) Hide();
            if (_registered && IsSquadron) _layer.InvalidateMembership(EntityId);
        }

        public void SetColor(Color color, bool selected)
        {
            _frame.color = color;
            color.a = selected ? 0.35f : 0.2f;
            _icon.color = color;
        }

        internal void Hide()
        {
            if (!IsShown) return;
            IsShown = false;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        internal void Display(Vector2 center, Rect viewport, float zoom, float hitSize,
            float blend, long groupId = 0, Vector2Int cell = default, float visualScale = 1f)
        {
            float half = hitSize * 0.5f;
            if (center.x < viewport.xMin - half || center.x > viewport.xMax + half ||
                center.y < viewport.yMin - half || center.y > viewport.yMax + half)
            {
                Hide();
                return;
            }

            if (!IsShown)
            {
                _layoutOffset = Vector2.zero;
                _hitSize = hitSize;
            }
            else if (_lastGroup != groupId || _lastCell != cell)
            {
                _layoutOffset = _lastLayout + _layoutOffset - center;
            }

            _lastGroup = groupId;
            _lastCell = cell;
            _lastLayout = center;
            _layoutOffset *= 1f - blend;
            if (_layoutOffset.sqrMagnitude <= HIT_SETTLE_PIXELS * HIT_SETTLE_PIXELS)
                _layoutOffset = Vector2.zero;
            _hitSize = Mathf.Lerp(_hitSize, hitSize, blend);
            if (Mathf.Abs(_hitSize - hitSize) <= HIT_SETTLE_PIXELS) _hitSize = hitSize;

            Vector2 local = center + _layoutOffset - viewport.min;
            if (!IsShown || (local - _root.anchoredPosition).sqrMagnitude > 0.25f)
                _root.anchoredPosition = local;
            float scale = zoom * visualScale;
            if (scale != _zoom)
            {
                _zoom = scale;
                _root.localScale = Vector3.one * scale;
            }

            Vector2 displayed = _root.anchoredPosition + viewport.min;
            Vector2 size = Vector2.one * _hitSize;
            ScreenRect = new Rect(displayed - size * 0.5f, size);
            if (!IsShown)
            {
                IsShown = true;
                _root.gameObject.SetActive(true);
            }
        }

        public bool ContainsScreenPoint(Vector2 screenPoint) => IsShown && ScreenRect.Contains(screenPoint);

        public bool OverlapsMarquee(MarqueeRectangle rectangle) =>
            IsShown && rectangle.MinX <= ScreenRect.xMax && rectangle.MaxX >= ScreenRect.xMin &&
            rectangle.MinY <= ScreenRect.yMax && rectangle.MaxY >= ScreenRect.yMin;
    }
}
