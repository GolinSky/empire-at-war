using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.StructureStatusIcons
{
    // Passive graphics: the presenter owns every update and the source-sized pool.
    public sealed class StructureStatusIconView
    {
        private readonly Image _icon;
        private readonly Image _outgoing;
        private Color _color;
        private float _alpha = -1f;
        private float _outgoingAlpha;
        private float _outgoingSeconds;
        public RectTransform Root { get; }

        private StructureStatusIconView(RectTransform root, Image icon, Image outgoing)
        {
            Root = root;
            _icon = icon;
            _outgoing = outgoing;
        }

        public static StructureStatusIconView Create(Transform parent, float size, int layer)
        {
            var root = new GameObject("StructureStatusIcon", typeof(RectTransform));
            root.layer = layer;
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = Vector2.one * size;
            Image outgoing = CreateImage("OutgoingIcon", rect, size, layer);
            outgoing.enabled = false;
            Image icon = CreateImage("Icon", rect, size, layer);
            root.SetActive(false);
            return new StructureStatusIconView(rect, icon, outgoing);
        }

        private static Image CreateImage(string name, RectTransform parent, float size, int layer)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.layer = layer;
            child.transform.SetParent(parent, false);
            Image image = child.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = Vector2.one * size;
            return image;
        }

        public void SetAppearance(StructureStatusIconArtwork artwork, StructureStatusIconState state)
        {
            Sprite sprite = artwork.GetSprite(state);
            Color color = artwork.GetColor(state);
            if (_icon.sprite == sprite && _color == color) return;
            if (_alpha > 0f)
            {
                _outgoingAlpha = _alpha;
                _outgoingSeconds = 0f;
                _outgoing.sprite = _icon.sprite;
                _outgoing.color = _icon.color;
                _outgoing.enabled = true;
            }
            _icon.sprite = sprite;
            _color = color;
            _alpha = -1f;
        }

        public void Display(bool visible, float alpha, Vector2 position, float seconds = 0f)
        {
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
            if (!visible)
            {
                _alpha = -1f;
                _outgoingAlpha = 0f;
                _outgoing.enabled = false;
                return;
            }
            if (Root.anchoredPosition != position) Root.anchoredPosition = position;
            if (_outgoingAlpha > 0f)
            {
                _outgoingSeconds += seconds;
                float outgoingAlpha = _outgoingAlpha * Mathf.Max(0f,
                    1f - _outgoingSeconds / StructureStatusIconModel.FADE_OUT_SECONDS);
                Color outgoingColor = _outgoing.color;
                outgoingColor.a = outgoingAlpha;
                _outgoing.color = outgoingColor;
                if (outgoingAlpha == 0f)
                {
                    _outgoingAlpha = 0f;
                    _outgoing.enabled = false;
                }
            }
            if (_alpha == alpha) return;
            _alpha = alpha;
            Color color = _color;
            color.a = alpha;
            _icon.color = color;
        }

        public void Release()
        {
            if (Root == null) return;
            if (Application.isPlaying) Object.Destroy(Root.gameObject);
            else Object.DestroyImmediate(Root.gameObject);
        }
    }
}
