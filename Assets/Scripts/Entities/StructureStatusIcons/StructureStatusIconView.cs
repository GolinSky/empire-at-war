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
        private Color _color;
        private float _alpha = -1f;
        public RectTransform Root { get; }

        private StructureStatusIconView(RectTransform root, Image glyph, Image badge, Image background)
        {
            Root = root;
            _glyph = glyph;
            _badge = badge;
            _background = background;
        }

        public static StructureStatusIconView Create(Transform parent, float size, int layer)
        {
            var root = new GameObject("StructureStatusIcon", typeof(RectTransform));
            root.layer = layer;
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = Vector2.one * size;
            Image glyph = CreateImage("Glyph", rect, Vector2.zero, size, layer);
            Image background = CreateImage("BadgeBackground", rect, new Vector2(size * 0.3f, -size * 0.3f), size * 0.55f, layer);
            Image badge = CreateImage("Badge", rect, new Vector2(size * 0.3f, -size * 0.3f), size * 0.45f, layer);
            root.SetActive(false);
            return new StructureStatusIconView(rect, glyph, badge, background);
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
            if (_glyph.sprite != glyph) _glyph.sprite = glyph;
            if (_badge.sprite != badge) _badge.sprite = badge;
            if (_color != color) { _color = color; _alpha = -1f; }
        }

        public void Display(bool visible, float alpha, Vector2 position)
        {
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
            if (!visible) return;
            if (Root.anchoredPosition != position) Root.anchoredPosition = position;
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
