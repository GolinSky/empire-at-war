using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>Applies a bottom-to-top black-to-white gradient to the icon's health tint.</summary>
    public sealed class HardPointIconImage : Image
    {
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            base.OnPopulateMesh(vertices);

            UIVertex vertex = default;
            float bottom = float.PositiveInfinity;
            float top = float.NegativeInfinity;
            for (int index = 0; index < vertices.currentVertCount; index++)
            {
                vertices.PopulateUIVertex(ref vertex, index);
                bottom = Mathf.Min(bottom, vertex.position.y);
                top = Mathf.Max(top, vertex.position.y);
            }

            for (int index = 0; index < vertices.currentVertCount; index++)
            {
                vertices.PopulateUIVertex(ref vertex, index);
                float brightness = Mathf.InverseLerp(bottom, top, vertex.position.y);
                Color tint = vertex.color;
                vertex.color = new Color(tint.r * brightness, tint.g * brightness, tint.b * brightness, tint.a);
                vertices.SetUIVertex(vertex, index);
            }
        }
    }
}
