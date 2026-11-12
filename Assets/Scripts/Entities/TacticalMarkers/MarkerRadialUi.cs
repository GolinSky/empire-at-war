using EmpireAtWar.Services.TacticalMarkers;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.TacticalMarkers
{
    public sealed class MarkerRadialUi : BaseUi
    {
        [SerializeField] private RectTransform radial;
        [SerializeField] private GameObject pointerBlocker;
        [SerializeField] private RectTransform[] slices;
        [SerializeField] private Image[] frames;
        [SerializeField] private Image[] glyphs;
        [SerializeField] private TMP_Text hub;
        [SerializeField] private WorldMarkerView worldMarkerPrefab;
        [SerializeField] private AudioSource audioSource;
        private MarkerRadialModel _model;
        private TacticalMarkerSettings _settings;

        public WorldMarkerView WorldMarkerPrefab => worldMarkerPrefab;
        public AudioSource AudioSource => audioSource;

        public void Initialize(MarkerRadialModel model, TacticalMarkerSettings settings)
        {
            _model = model;
            _settings = settings;
            for (int i = 0; i < glyphs.Length; i++) glyphs[i].sprite = settings.GetIcon((TacticalMarkerType)i);
            _model.Changed += Render;
            Render();
        }

        public void SetOrigin(Vector2 screenPoint, string binding)
        {
            var parent = (RectTransform)radial.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                screenPoint, null, out Vector2 local);
            float marginX = Mathf.Min(170f, parent.rect.width * 0.5f);
            float marginY = Mathf.Min(170f, parent.rect.height * 0.5f);
            local.x = Mathf.Clamp(local.x, parent.rect.xMin + marginX, parent.rect.xMax - marginX);
            local.y = Mathf.Clamp(local.y, parent.rect.yMin + marginY, parent.rect.yMax - marginY);
            radial.anchoredPosition = local;
            hub.text = binding + "\nPING";
        }

        public Vector2 GetPointerOffset(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(radial, screenPoint, null, out Vector2 local);
            return local;
        }

        private void Render()
        {
            radial.gameObject.SetActive(_model.IsOpen);
            pointerBlocker.SetActive(_model.IsOpen);
            for (int i = 0; i < slices.Length; i++)
            {
                Color color = _settings.GetColor((TacticalMarkerType)i);
                frames[i].color = color * (i == _model.SelectedIndex ? 1f : 0.65f);
                glyphs[i].color = color;
                slices[i].localScale = Vector3.one * (i == _model.SelectedIndex ? 1.1f : 1f);
            }
        }

        public void Dispose() { if (_model != null) _model.Changed -= Render; }
        private void OnDestroy() => Dispose();
    }
}
