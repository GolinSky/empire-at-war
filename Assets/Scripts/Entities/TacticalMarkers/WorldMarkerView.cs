using TMPro;
using UnityEngine;

namespace EmpireAtWar.Entities.TacticalMarkers
{
    public sealed class WorldMarkerView : MonoBehaviour
    {
        [SerializeField] private Transform billboard;
        [SerializeField] private SpriteRenderer frame;
        [SerializeField] private SpriteRenderer glyph;
        [SerializeField] private TMP_Text sender;
        [SerializeField] private Transform ring;
        [SerializeField] private MeshRenderer ringRenderer;
        [SerializeField] private LineRenderer beam;
        [SerializeField] private LineRenderer pulse;
        private MaterialPropertyBlock _properties;
        private Color _color;
        private Color _senderColor;

        private void Awake() => _properties = new MaterialPropertyBlock();

        public void Show(Sprite icon, Sprite octagon, Color color, string senderName, Color senderColor)
        {
            _color = color; _senderColor = senderColor;
            frame.sprite = octagon; glyph.sprite = icon;
            sender.text = senderName;
            gameObject.SetActive(true);
        }

        public void Render(Vector3 point, UnityEngine.Camera camera, float age, float remaining)
        {
            transform.position = point;
            float scale = Mathf.Clamp(Vector3.Distance(camera.transform.position, point) * 0.032f, 20f, 110f);
            billboard.localPosition = Vector3.up * 90f;
            billboard.rotation = camera.transform.rotation;
            billboard.localScale = Vector3.one * scale;
            float alpha = Mathf.Clamp01(remaining);
            Color color = _color; color.a *= alpha;
            frame.color = color; glyph.color = color;
            Color senderColor = _senderColor; senderColor.a *= alpha; sender.color = senderColor;
            _properties.SetColor("_Color", color);
            ringRenderer.SetPropertyBlock(_properties);
            ring.localRotation = Quaternion.Euler(0f, age * 15f, 0f);
            beam.startColor = color * new Color(1f, 1f, 1f, 0.2f); beam.endColor = color;
            pulse.gameObject.SetActive(age < 0.6f);
            if (age < 0.6f)
            {
                pulse.transform.localScale = Vector3.one * (1f + age * 2f);
                color.a *= 1f - age / 0.6f;
                pulse.startColor = color; pulse.endColor = color;
            }
        }

        public void Release() => gameObject.SetActive(false);
    }
}
