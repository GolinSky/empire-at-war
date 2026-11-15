using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>
    /// Stores frame-local coordinates and the ownership style in UV1 so both marker types
    /// can use the same material on the shared <see cref="BattleMarkerLayer"/> canvas.
    /// </summary>
    public sealed class SquadronFrameMeshEffect : BaseMeshEffect
    {
        internal bool UseTeamFrame { get; set; }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            Vector2 center = graphic.rectTransform.rect.center;
            UIVertex vertex = default;
            for (int i = 0; i < vertexHelper.currentVertCount; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                Vector2 local = (Vector2)vertex.position - center;
                vertex.uv1 = new Vector4(local.x, local.y, UseTeamFrame ? 1f : 0f, 0f);
                vertexHelper.SetUIVertex(vertex, i);
            }
        }
    }
}
