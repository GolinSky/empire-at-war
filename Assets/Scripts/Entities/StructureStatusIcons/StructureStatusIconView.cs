using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.StructureStatusIcons
{
    // Passive graphics: the presenter owns every update and the source-sized pool.
    public sealed class StructureStatusIconView
    {
        private readonly Image _glyph;
        private readonly Image _badge;
        private readonly Image _background;
        private readonly Image[] _outgoing;
        private Color _color;
        private float _alpha = -1f;
        private float _outgoingAlpha;
        private float _outgoingSeconds;
        public RectTransform Root { get; }

        private StructureStatusIconView(RectTransform root, Image glyph, Image badge, Image background, Image[] outgoing)
        {
            Root = root;
            _glyph = glyph;
            _badge = badge;
            _background = background;
            _outgoing = outgoing;
        }

        public static StructureStatusIconView Create(Transform parent, float size, int layer)
        {
            var root = new GameObject("StructureStatusIcon", typeof(RectTransform));
            root.layer = layer;
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = Vector2.one * size;
            var outgoing = new[]
            {
                CreateImage("OutgoingGlyph", rect, Vector2.zero, size, layer),
                CreateImage("OutgoingBackground", rect, new Vector2(size * 0.3f, -size * 0.3f), size * 0.55f, layer),
                CreateImage("OutgoingBadge", rect, new Vector2(size * 0.3f, -size * 0.3f), size * 0.45f, layer)
            };
            foreach (Image image in outgoing) image.enabled = false;
            Image glyph = CreateImage("Glyph", rect, Vector2.zero, size, layer);
            Image background = CreateImage("BadgeBackground", rect, new Vector2(size * 0.3f, -size * 0.3f), size * 0.55f, layer);
            Image badge = CreateImage("Badge", rect, new Vector2(size * 0.3f, -size * 0.3f), size * 0.45f, layer);
            root.SetActive(false);
            return new StructureStatusIconView(rect, glyph, badge, background, outgoing);
        }

        private static Image CreateImage(string name, RectTransform parent, Vector2 position, float size, int layer)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.layer = layer;
            child.transform.SetParent(parent, false);
            Image image = child.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = Vector2.one * size;
            image.rectTransform.anchoredPosition = position;
            return image;
        }

        public void SetAppearance(StructureStatusIconArtwork artwork, StructureStatusIconKind kind,
            StructureStatusIconState state)
        {
            Sprite glyph = artwork.GetGlyph(kind);
            Sprite badge = artwork.GetBadge(state);
            Color color = artwork.GetColor(state);
            if ((_glyph.sprite != glyph || _badge.sprite != badge || _color != color) && _alpha > 0f)
            {
                _outgoingAlpha = _alpha;
                _outgoingSeconds = 0f;
                CopyAppearance(_glyph, _outgoing[0]);
                CopyAppearance(_background, _outgoing[1]);
                CopyAppearance(_badge, _outgoing[2]);
            }
            if (_glyph.sprite != glyph) _glyph.sprite = glyph;
            if (_badge.sprite != badge) _badge.sprite = badge;
            if (_color != color) { _color = color; _alpha = -1f; }
        }

        private static void CopyAppearance(Image source, Image destination)
        {
            destination.sprite = source.sprite;
            destination.color = source.color;
            destination.enabled = true;
        }

        public void Display(bool visible, float alpha, Vector2 position, float seconds = 0f)
        {
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
            if (!visible)
            {
                _alpha = -1f;
                _outgoingAlpha = 0f;
                foreach (Image image in _outgoing) image.enabled = false;
                return;
            }
            if (Root.anchoredPosition != position) Root.anchoredPosition = position;
            if (_outgoingAlpha > 0f)
            {
                _outgoingSeconds += seconds;
                float outgoingAlpha = _outgoingAlpha * Mathf.Max(0f,
                    1f - _outgoingSeconds / StructureStatusIconModel.FADE_OUT_SECONDS);
                foreach (Image image in _outgoing)
                {
                    Color outgoingColor = image.color;
                    outgoingColor.a = outgoingAlpha;
                    image.color = outgoingColor;
                    image.enabled = outgoingAlpha > 0f;
                }
                if (outgoingAlpha == 0f) _outgoingAlpha = 0f;
            }
            if (_alpha == alpha) return;
            _alpha = alpha;
            Color color = _color;
            color.a = alpha;
            _glyph.color = color;
            _badge.color = color;
            _background.color = new Color(16f / 255f, 26f / 255f, 41f / 255f, alpha);
        }

        public void Release()
        {
            if (Root == null) return;
            if (Application.isPlaying) Object.Destroy(Root.gameObject);
            else Object.DestroyImmediate(Root.gameObject);
        }
    }
}
