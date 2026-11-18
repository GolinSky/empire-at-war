using System;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Models.Players;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    public sealed class BattleMarker : IBattleMarker, IDisposable
    {
        private const float SMOOTH_TIME = 0.1f;
        private readonly IBattleMarkerLayer _layer;
        private readonly RectTransform _root;
        private readonly Image _frame;
        private readonly Image _icon;
        private readonly float _visualSize;
        private bool _registered;
        private float _scale;
        private long _layoutKey;
        private ScreenSpring _follow;
        private Vector2 _displayed;

        public long EntityId { get; private set; }
        public bool IsSquadron { get; }
        public bool IsShown { get; private set; }
        public Rect ScreenRect { get; private set; }
        public int DrawOrder { get; internal set; }

        internal readonly float ScreenOffset;
        internal float VisualSize => _visualSize;
        internal Transform AnchorTransform { get; set; }
        internal Vector3 Position => IsSquadron ? _position : AnchorTransform.position;
        internal PlayerId Owner { get; private set; }
        internal bool IsVisible { get; private set; }
        internal Action SampleVisibility { get; set; }
        internal Vector2 ProjectedAnchor { get; set; }
        internal int ClusterSlot { get; set; }
        internal int Rank { get; set; }

        private Vector3 _position;

        public BattleMarker(IBattleMarkerLayer layer, RectTransform root, Image frame, Image icon,
            float visualSize = 72f, float screenOffset = 60f, bool isSquadron = false)
        {
            _layer = layer;
            _root = root;
            _frame = frame;
            _icon = icon;
            _visualSize = visualSize;
            ScreenOffset = screenOffset;
            IsSquadron = isSquadron;
        }

        public void Register(long entityId, PlayerId owner)
        {
            EntityId = entityId;
            Owner = owner;
            _layer.Attach(_root, IsSquadron);
            _root.sizeDelta = Vector2.one * _visualSize;
            _root.gameObject.SetActive(false);
            _scale = 0f;
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
            if (_registered) _layer.InvalidateMembership(EntityId);
            Hide();
        }

        public void SetVisible(bool visible)
        {
            if (IsVisible == visible) return;
            IsVisible = visible;
            if (!visible) Hide();
            if (_registered) _layer.InvalidateMembership(EntityId);
        }

        public void SetColor(Color color, bool selected)
        {
            // The frame shader uses alpha to distinguish selected markers on the shared material.
            color.a = selected ? 1f : 0.5f;
            _frame.color = color;
            _icon.color = Color.white;
        }

        internal void Hide()
        {
            if (!IsShown) return;
            IsShown = false;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Draws at <paramref name="target"/> plus a decaying offset. A new <paramref name="layoutKey"/>
        /// (cluster, rank or membership change) folds the jump into the offset so motion stays continuous.
        /// </summary>
        internal void Display(Vector2 target, Rect viewport, float scale, long layoutKey, float deltaTime)
        {
            float half = _visualSize * scale * 0.5f;
            if (!IsShown)
            {
                _follow = default;
            }
            else if (layoutKey != _layoutKey)
            {
                _follow.Value = _displayed - target;
            }

            _layoutKey = layoutKey;
            _follow.Step(Vector2.zero, SMOOTH_TIME, deltaTime);
            _displayed = target + _follow.Value;
            if (_displayed.x < viewport.xMin - half || _displayed.x > viewport.xMax + half ||
                _displayed.y < viewport.yMin - half || _displayed.y > viewport.yMax + half)
            {
                Hide();
                return;
            }

            _root.anchoredPosition = _displayed - viewport.min;
            if (scale != _scale)
            {
                _scale = scale;
                _root.localScale = Vector3.one * scale;
            }

            Vector2 size = Vector2.one * (half * 2f);
            ScreenRect = new Rect(_displayed - size * 0.5f, size);
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
