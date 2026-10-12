using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>
    /// Writes each frame vertex's position relative to the frame centre into UV1. The Squadron Frame shader draws
    /// its rounded border from it: once every marker is batched into the shared <see cref="SquadronIconLayer"/>
    /// canvas, vertex positions are screen positions and no longer centred on the marker.
    /// </summary>
    public sealed class SquadronFrameMeshEffect : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            Vector2 center = graphic.rectTransform.rect.center;
            UIVertex vertex = default;
            for (int i = 0; i < vertexHelper.currentVertCount; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                vertex.uv1 = (Vector2)vertex.position - center;
                vertexHelper.SetUIVertex(vertex, i);
            }
        }
    }
}
