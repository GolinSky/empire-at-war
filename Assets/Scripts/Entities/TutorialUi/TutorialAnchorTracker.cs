using System.Collections.Generic;
using EmpireAtWar.Services.Tutorial;
using UnityEngine;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>
    /// Finds the visible anchors of a hint, measures their screen bounds and keeps exactly those highlighted.
    /// </summary>
    public sealed class TutorialAnchorTracker
    {
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly List<TutorialAnchor> _anchors = new List<TutorialAnchor>();
        private readonly List<TutorialAnchor> _highlighted = new List<TutorialAnchor>();

        /// <summary>Anchors found by the last <see cref="Track"/>.</summary>
        public IReadOnlyList<TutorialAnchor> Anchors => _anchors;

        /// <returns>True when an anchor (or the fallback) is visible on screen; only then are they highlighted.</returns>
        public bool Track(TutorialAnchorId anchor, TutorialAnchorId fallback, out Rect screenRect)
        {
            bool hasAnchor = TutorialAnchor.TryFindAll(anchor, _anchors) ||
                             TutorialAnchor.TryFindAll(fallback, _anchors);
            screenRect = hasAnchor ? ScreenRect() : default;
            hasAnchor &= screenRect.Overlaps(new Rect(0f, 0f, Screen.width, Screen.height));
            if (hasAnchor) Highlight();
            else Clear();
            return hasAnchor;
        }

        public void Clear()
        {
            foreach (TutorialAnchor anchor in _highlighted)
                if (anchor != null) anchor.StopHighlight();
            _highlighted.Clear();
        }

        private void Highlight()
        {
            foreach (TutorialAnchor anchor in _highlighted)
                if (anchor != null && !_anchors.Contains(anchor))
                    anchor.StopHighlight();
            _highlighted.Clear();
            _highlighted.AddRange(_anchors);
            foreach (TutorialAnchor anchor in _highlighted) anchor.Highlight();
        }

        private Rect ScreenRect()
        {
            // UI canvases are Screen Space Overlay, so world corners are screen pixels.
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (TutorialAnchor anchor in _anchors)
            {
                anchor.Target.GetWorldCorners(_corners);
                min = Vector2.Min(min, _corners[0]);
                max = Vector2.Max(max, _corners[2]);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
