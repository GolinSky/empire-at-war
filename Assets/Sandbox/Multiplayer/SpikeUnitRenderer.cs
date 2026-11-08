using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Instanced boxes per side; capitals large, squadrons small. Client-only presentation.
    public sealed class SpikeUnitRenderer : MonoBehaviour
    {
        private static readonly Vector3 CAPITAL_SCALE = new Vector3(20f, 8f, 60f);
        private static readonly Vector3 SQUADRON_SCALE = new Vector3(8f, 3f, 8f);

        [SerializeField] private Mesh mesh;
        [SerializeField] private Material[] sideMaterials;

        private readonly List<Matrix4x4>[] _matrices = { new List<Matrix4x4>(), new List<Matrix4x4>() };

        public void Render(SpikeClientReplica replica)
        {
            _matrices[0].Clear();
            _matrices[1].Clear();
            IReadOnlyList<SpikeKinematics> rendered = replica.Rendered;
            for (int i = 0; i < rendered.Count; i++)
            {
                if (!replica.IsVisible(i))
                    continue;

                SpikeKinematics unit = rendered[i];
                Vector3 scale = replica.UnitKinds[i] == SpikeUnitKind.Capital ? CAPITAL_SCALE : SQUADRON_SCALE;
                Matrix4x4 matrix = Matrix4x4.TRS(
                    new Vector3(unit.Position.x, 0f, unit.Position.y),
                    Quaternion.Euler(0f, unit.Heading, 0f),
                    scale);
                _matrices[Mathf.Min(replica.UnitSides[i], 1)].Add(matrix);
            }

            for (int side = 0; side < _matrices.Length; side++)
            {
                if (_matrices[side].Count > 0)
                    Graphics.RenderMeshInstanced(new RenderParams(sideMaterials[side]), mesh, 0, _matrices[side]);
            }
        }
    }
}
