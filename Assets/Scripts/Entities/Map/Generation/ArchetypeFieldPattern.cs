using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Planet-seeded macro stamps. FieldDensity cuts all mandatory clearings and ship-width lanes first.</summary>
    public static class ArchetypeFieldPattern
    {
        public static bool IsGuaranteedGap(Vector3 position, MapSizeSettings size)
        {
            if (size.Archetype != MapArchetypeKind.AsteroidBelt) return false;
            Vector3 local = Quaternion.Euler(0f, -size.SkeletonAngle, 0f) *
                (position - PlanarGeometry.GetCenter(size.Bounds));
            float half = Mathf.Min(size.Bounds.Max.x - size.Bounds.Min.x,
                size.Bounds.Max.y - size.Bounds.Min.y) * 0.5f;
            float gap = size.MinLaneWidth * 0.5f + size.FieldCellSize;
            float x = Mathf.Abs(local.x);
            return Mathf.Abs(local.z) < half * 0.22f + size.FieldCellSize &&
                (x <= gap || Mathf.Abs(x - half * 0.62f) <= gap);
        }

        public static bool IsField(Vector3 position, MapSizeSettings size, float noise, float ridge)
        {
            if (IsGuaranteedGap(position, size)) return false;
            foreach (FieldVolume wall in size.StructuralFields)
                if (PlanarGeometry.Distance(position, wall.Center) < wall.Radius) return true;
            Vector3 local = Quaternion.Euler(0f, -size.SkeletonAngle, 0f) *
                (position - PlanarGeometry.GetCenter(size.Bounds));
            float half = Mathf.Min(size.Bounds.Max.x - size.Bounds.Min.x,
                size.Bounds.Max.y - size.Bounds.Min.y) * 0.5f;
            float x = Mathf.Abs(local.x);
            float z = Mathf.Abs(local.z);
            float radius = new Vector2(local.x, local.z).magnitude;
            float gap = size.MinLaneWidth * 0.5f + size.FieldCellSize;
            switch (size.Archetype)
            {
                case MapArchetypeKind.AsteroidBelt:
                    // Three stable crossings: centre and two flanks.
                    if (z < half * 0.22f) return true;
                    return noise > size.FieldThreshold + 0.12f;
                case MapArchetypeKind.TwinPass:
                    return z < half * 0.28f && x < half * 0.5f ||
                        noise > size.FieldThreshold + 0.12f;
                case MapArchetypeKind.OrbitalRing:
                    return Mathf.Abs(radius - half * 0.47f) < half * 0.12f && x > gap && z > gap ||
                        noise > size.FieldThreshold + 0.16f;
                case MapArchetypeKind.DenseField:
                    return noise > size.FieldThreshold || ridge > size.RidgeThreshold;
                case MapArchetypeKind.NebulaCorridors:
                    return noise > size.FieldThreshold + 0.2f;
                case MapArchetypeKind.Islands:
                    float clusterX = x - half * 0.48f;
                    float clusterZ = z - half * 0.48f;
                    return clusterX * clusterX + clusterZ * clusterZ < half * half * 0.075f &&
                        noise > size.FieldThreshold - 0.2f;
                default:
                    return noise > size.FieldThreshold || ridge > size.RidgeThreshold;
            }
        }
    }
}
