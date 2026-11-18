using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>Pooled thin lines tying a displaced grid back to its squadrons.</summary>
    internal sealed class SquadronLeaderLines
    {
        private const float THICKNESS = 1f;
        private static readonly Color LINE_COLOR = new Color(1f, 1f, 1f, 0.25f);

        private readonly List<RectTransform> _lines = new List<RectTransform>();
        private readonly RectTransform _parent;
        private int _used;

        public SquadronLeaderLines(RectTransform parent) => _parent = parent;

        public void Begin() => _used = 0;

        public void Draw(Vector2 from, Vector2 to, Rect viewport)
        {
            if (_used == _lines.Count) _lines.Add(CreateLine());
            RectTransform line = _lines[_used++];
            Vector2 delta = to - from;
            line.anchoredPosition = (from + to) * 0.5f - viewport.min;
            line.sizeDelta = new Vector2(delta.magnitude, THICKNESS);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            if (!line.gameObject.activeSelf) line.gameObject.SetActive(true);
        }

        public void End()
        {
            for (int i = _used; i < _lines.Count; i++)
                if (_lines[i].gameObject.activeSelf) _lines[i].gameObject.SetActive(false);
        }

        private RectTransform CreateLine()
        {
            var line = new GameObject("LeaderLine", typeof(RectTransform));
            line.layer = _parent.gameObject.layer;
            var rect = (RectTransform)line.transform;
            rect.SetParent(_parent, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = line.AddComponent<Image>();
            image.color = LINE_COLOR;
            image.raycastTarget = false;
            return rect;
        }
    }
}
