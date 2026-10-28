using UnityEngine;

namespace EmpireAtWar.Components.Environment
{
    /// <summary>Bounds a fixed-budget, shader-animated dust field to the playable map.</summary>
    public sealed class MapSpaceDust : MonoBehaviour
    {
        private static readonly int _mapBoundsId = Shader.PropertyToID("_MapBounds");
        private static readonly int _heightRangeId = Shader.PropertyToID("_HeightRange");
        private const float BILLBOARD_MARGIN = 1720f;

        [SerializeField] private MeshRenderer fieldRenderer;
        [SerializeField, Tooltip("World-space altitude range, including the deepest ship tier and hull margins.")]
        private Vector2 heightRange = new Vector2(-650f, 180f);

        public void Configure(Vector2 min, Vector2 max)
        {
            var properties = new MaterialPropertyBlock();
            properties.SetVector(_mapBoundsId, new Vector4(min.x, min.y, max.x, max.y));
            properties.SetVector(_heightRangeId, new Vector4(heightRange.x, heightRange.y, 0f, 0f));
            fieldRenderer.SetPropertyBlock(properties);

            // Vertices are positioned in world space by the shader, independent of the prefab transform.
            fieldRenderer.bounds = new Bounds(
                new Vector3((min.x + max.x) * 0.5f, (heightRange.x + heightRange.y) * 0.5f,
                    (min.y + max.y) * 0.5f),
                new Vector3(max.x - min.x, heightRange.y - heightRange.x, max.y - min.y)
                + Vector3.one * BILLBOARD_MARGIN);
            fieldRenderer.enabled = true;
        }
    }
}
