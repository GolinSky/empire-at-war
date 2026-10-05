using EmpireAtWar.Services.Layer;
using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class StructureSpawnClearance : IStructureSpawnClearance
    {
        private readonly int _obstacleMask;

        public float Radius { get; }

        public StructureSpawnClearance(ILayerService layerService, BoxCollider[] structurePrefabs)
        {
            _obstacleMask = layerService.GetMask(LayerKey.Unit, LayerKey.Obstacle);
            foreach (BoxCollider prefab in structurePrefabs)
            {
                Bounds bounds = GetSpawnBounds(prefab);
                Vector3 reach = bounds.extents + new Vector3(
                    Mathf.Abs(bounds.center.x), Mathf.Abs(bounds.center.y), Mathf.Abs(bounds.center.z));
                // Enclose the entire scaled collider, including its offset from the spawn pivot.
                Radius = Mathf.Max(Radius, reach.magnitude);
            }
        }

        public bool IsClear(Vector3 position) =>
            !Physics.CheckSphere(position, Radius, _obstacleMask, QueryTriggerInteraction.Ignore);

        /// <summary>Bounds of a prefab collider around the spawn pivot, keeping its rotation and scale.</summary>
        public static Bounds GetSpawnBounds(BoxCollider prefab)
        {
            // Entity initialization replaces the prefab position, preserving its rotation and scale.
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero,
                prefab.transform.localRotation, prefab.transform.localScale);
            Bounds bounds = new Bounds(matrix.MultiplyPoint3x4(prefab.center), Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 sign = new Vector3(
                    (corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f);
                bounds.Encapsulate(matrix.MultiplyPoint3x4(
                    prefab.center + Vector3.Scale(prefab.size * 0.5f, sign)));
            }

            return bounds;
        }
    }
}
