using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Hazards
{
    /// <summary>Generated description of one cloud: lobes plus the noise that roughens their outline.
    /// <see cref="HazardShape"/> bakes it into the shared gameplay and render geometry.</summary>
    public readonly struct HazardSpot
    {
        // Noise amplitudes as shares of Roughness; the outline stays within Spread of the lobe capsules.
        public const float WARP_SHARE = 0.22f;
        public const float DETAIL_SHARE = 0.06f;
        public const float BLEND_SHARE = 0.3f;
        public const float NOISE_SIZE_SHARE = 1.1f;

        public SpaceHazardData Data { get; }
        public IReadOnlyList<HazardLobe> Lobes { get; }
        public HazardFrame Frame { get; }
        /// <summary>Reference size in metres for the outline noise; zero keeps plain lobe ellipses.</summary>
        public float Roughness { get; }
        public float CellSize { get; }
        /// <summary>How far the noisy outline can reach past the lobe capsules.</summary>
        public float Spread { get; }
        public Vector3 Center => Frame.Origin;

        public HazardSpot(SpaceHazardData data, IReadOnlyList<HazardLobe> lobes, HazardFrame frame,
            float roughness, float cellSize)
        {
            Data = data;
            Lobes = lobes;
            Frame = frame;
            Roughness = roughness;
            CellSize = cellSize;
            float aspect = 1f;
            foreach (HazardLobe lobe in lobes)
                aspect = Mathf.Max(aspect, lobe.Length / lobe.Width);
            // Warp moves sample points; detail and the smooth union lift the field, which on a lobe's
            // long axis moves the outline by up to its aspect ratio times as far.
            Spread = roughness * (WARP_SHARE + (DETAIL_SHARE + BLEND_SHARE * 0.25f) * aspect);
        }

        /// <summary>Conservative distance from a point to the cloud outline.</summary>
        public float GetClearance(Vector3 position)
        {
            float distance = float.PositiveInfinity;
            foreach (HazardLobe lobe in Lobes)
                distance = Mathf.Min(distance, lobe.GetCapsuleDistance(position));
            return distance - Spread;
        }

        public float GetClearance(HazardSpot other)
        {
            float distance = float.PositiveInfinity;
            foreach (HazardLobe lobe in Lobes)
                foreach (HazardLobe otherLobe in other.Lobes)
                    distance = Mathf.Min(distance, lobe.GetCapsuleDistance(otherLobe));
            return distance - Spread - other.Spread;
        }

        /// <summary>Planar box the outline can reach.</summary>
        public Bounds GetReach()
        {
            Bounds bounds = new Bounds(Lobes[0].Center, Vector3.zero);
            foreach (HazardLobe lobe in Lobes)
            {
                Vector3 reach = lobe.Direction * (lobe.Length - lobe.Width);
                Vector3 radius = new Vector3(lobe.Width + Spread, 0f, lobe.Width + Spread);
                bounds.Encapsulate(lobe.Center - reach - radius);
                bounds.Encapsulate(lobe.Center - reach + radius);
                bounds.Encapsulate(lobe.Center + reach - radius);
                bounds.Encapsulate(lobe.Center + reach + radius);
            }
            return bounds;
        }

        public HazardSpot Mirror(Vector3 pivot)
        {
            HazardLobe[] lobes = new HazardLobe[Lobes.Count];
            for (int i = 0; i < lobes.Length; i++)
                lobes[i] = Lobes[i].Mirror(pivot);
            return new HazardSpot(Data, lobes, Frame.Mirror(pivot), Roughness, CellSize);
        }

        public HazardSpot Translate(Vector3 offset)
        {
            HazardLobe[] lobes = new HazardLobe[Lobes.Count];
            for (int i = 0; i < lobes.Length; i++)
                lobes[i] = new HazardLobe(Lobes[i].Center + offset, Lobes[i].Direction, Lobes[i].Length, Lobes[i].Width);
            HazardFrame frame = new HazardFrame(Frame.Origin + offset, Frame.Along, Frame.IsFolded, Frame.NoiseOffset);
            return new HazardSpot(Data, lobes, frame, Roughness, CellSize);
        }

        public HazardSpot WithData(SpaceHazardData data)
        {
            return new HazardSpot(data, Lobes, Frame, Roughness, CellSize);
        }
    }
}
