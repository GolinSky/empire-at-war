using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ui
{
    /// <summary>Pulses this graphic's own tint and material brightness without replacing its live button state.</summary>
    [DisallowMultipleComponent]
    public sealed class UiHighlight : MonoBehaviour, IUiHighlight, IMeshModifier, IMaterialModifier
    {
        private const float PULSE_SECONDS = 1.2f;
        private static readonly int _colorId = Shader.PropertyToID("_Color");
        [SerializeField] private Graphic targetGraphic;

        private bool _isHighlighted;
        private float _startedAt;
        private float _pulse;
        private Material _highlightMaterial;

        public void Highlight()
        {
            if (_isHighlighted) return;
            _isHighlighted = true;
            _startedAt = Time.unscaledTime;
            _pulse = 0f;
            targetGraphic.SetVerticesDirty();
            targetGraphic.SetMaterialDirty();
        }

        public void StopHighlight()
        {
            if (!_isHighlighted) return;
            _isHighlighted = false;
            targetGraphic.SetVerticesDirty();
            targetGraphic.SetMaterialDirty();
        }

        private void Update()
        {
            if (!_isHighlighted) return;
            _pulse = 0.5f - 0.5f * Mathf.Cos((Time.unscaledTime - _startedAt) *
                (2f * Mathf.PI / PULSE_SECONDS));
            targetGraphic.SetVerticesDirty();
            targetGraphic.SetMaterialDirty();
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!_isHighlighted || !isActiveAndEnabled) return baseMaterial;
            if (_highlightMaterial == null)
                _highlightMaterial = new Material(baseMaterial) { hideFlags = HideFlags.HideAndDontSave };
            // Refresh stencil and gradient properties from the current button material, never the shared asset.
            _highlightMaterial.CopyPropertiesFromMaterial(baseMaterial);
            Color tint = baseMaterial.GetColor(_colorId);
            float brightness = Mathf.Lerp(1.8f, 3.5f, _pulse);
            _highlightMaterial.SetColor(_colorId,
                new Color(tint.r * brightness, tint.g * brightness, tint.b * brightness, tint.a));
            return _highlightMaterial;
        }

        public void ModifyMesh(VertexHelper mesh)
        {
            if (!_isHighlighted || !isActiveAndEnabled) return;
            Color tint = Color.Lerp(new Color(0.22f, 0.74f, 0.97f),
                new Color(0.82f, 0.97f, 1f), _pulse);
            float strength = Mathf.Lerp(0.45f, 0.85f, _pulse);
            UIVertex vertex = default;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                Color original = vertex.color;
                Color highlighted = Color.Lerp(original, tint, strength);
                highlighted.a = original.a;
                vertex.color = highlighted;
                mesh.SetUIVertex(vertex, i);
            }
        }

        public void ModifyMesh(Mesh mesh)
        {
            using (var vertices = new VertexHelper(mesh))
            {
                ModifyMesh(vertices);
                vertices.FillMesh(mesh);
            }
        }

        private void OnDisable() => StopHighlight();

        private void OnDestroy()
        {
            if (_highlightMaterial == null) return;
            if (Application.isPlaying) Destroy(_highlightMaterial);
            else DestroyImmediate(_highlightMaterial);
        }
    }
}
