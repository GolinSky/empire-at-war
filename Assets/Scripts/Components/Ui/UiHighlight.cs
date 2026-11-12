using MPUIKIT;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ui
{
    /// <summary>
    /// Pulses this graphic from its own colour to the opposite hue and back, without replacing its live button state.
    /// Material tint and MPImage gradients fade to white with the pulse, so the opposite colour shows as authored.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiHighlight : MonoBehaviour, IUiHighlight, IMeshModifier, IMaterialModifier
    {
        private const float PULSE_SECONDS = 1.2f;
        private const float OPPOSITE_HUE_SHIFT = 0.5f;
        // Keep the opposite colour saturated and mid-bright: dark UI gets a visible colour, white text stays readable.
        private const float MIN_SATURATION = 0.7f;
        private const float MIN_VALUE = 0.65f;
        private const float MAX_VALUE = 0.8f;
        private const int CORNER_GRADIENT_COLORS = 4;
        private const int GRADIENT_COLORS = 8;
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");
        private static readonly int[] GRADIENT_COLOR_IDS = CreateGradientColorIds();

        [SerializeField] private Graphic targetGraphic;

        private bool _isHighlighted;
        private float _startedAt;
        private float _pulse;
        private Color _opposite;
        private Material _highlightMaterial;

        public void Highlight()
        {
            if (_isHighlighted) return;
            _isHighlighted = true;
            _startedAt = Time.unscaledTime;
            _pulse = 0f;
            Refresh();
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
            Refresh();
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!_isHighlighted || !isActiveAndEnabled) return baseMaterial;
            if (_highlightMaterial == null)
                _highlightMaterial = new Material(baseMaterial) { hideFlags = HideFlags.HideAndDontSave };
            // Refresh stencil and gradient properties from the current material, never the shared asset.
            _highlightMaterial.CopyPropertiesFromMaterial(baseMaterial);
            FadeToWhite(_highlightMaterial, COLOR_ID);
            foreach (int id in GRADIENT_COLOR_IDS) FadeToWhite(_highlightMaterial, id);
            return _highlightMaterial;
        }

        public void ModifyMesh(VertexHelper mesh)
        {
            if (!_isHighlighted || !isActiveAndEnabled) return;
            UIVertex vertex = default;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                Color original = vertex.color;
                Color highlighted = Color.Lerp(original, _opposite, _pulse);
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

        // Live button colours and gradients can change while highlighted (affordable, disabled).
        private void Refresh()
        {
            _opposite = Opposite(VisibleColor());
            targetGraphic.SetVerticesDirty();
            targetGraphic.SetMaterialDirty();
        }

        /// <summary>The graphic's colour as drawn: vertex colour × material tint × average gradient colour.</summary>
        private Color VisibleColor()
        {
            Material material = targetGraphic.material;
            Color color = targetGraphic.color;
            if (material.HasProperty(COLOR_ID)) color *= material.GetColor(COLOR_ID);
            if (targetGraphic is MPImage image && image.GradientEffect.Enabled)
                color *= AverageGradientColor(image.GradientEffect);
            return color;
        }

        private static Color AverageGradientColor(GradientEffect gradient)
        {
            Color sum = Color.clear;
            if (gradient.GradientType == GradientType.Corner)
            {
                foreach (Color corner in gradient.CornerGradientColors) sum += corner;
                return sum / gradient.CornerGradientColors.Length;
            }

            GradientColorKey[] keys = gradient.Gradient.colorKeys;
            foreach (GradientColorKey key in keys) sum += key.color;
            return sum / keys.Length;
        }

        private static Color Opposite(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            return Color.HSVToRGB(Mathf.Repeat(hue + OPPOSITE_HUE_SHIFT, 1f), Mathf.Max(saturation, MIN_SATURATION),
                Mathf.Clamp(value, MIN_VALUE, MAX_VALUE));
        }

        private void FadeToWhite(Material material, int id)
        {
            if (!material.HasProperty(id)) return;
            Color color = material.GetColor(id);
            Color faded = Color.Lerp(color, Color.white, _pulse);
            faded.a = color.a;
            material.SetColor(id, faded);
        }

        private static int[] CreateGradientColorIds()
        {
            int[] ids = new int[CORNER_GRADIENT_COLORS + GRADIENT_COLORS];
            for (int i = 0; i < CORNER_GRADIENT_COLORS; i++)
                ids[i] = Shader.PropertyToID($"_CornerGradientColor{i}");
            for (int i = 0; i < GRADIENT_COLORS; i++)
                ids[CORNER_GRADIENT_COLORS + i] = Shader.PropertyToID($"_GradientColor{i}");
            return ids;
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
