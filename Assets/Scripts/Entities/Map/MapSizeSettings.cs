using System;
using System.Collections.Generic;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Entities.Map
{
    [Serializable]
    public sealed class MapSizeSettings
    {
        public bool IsArchetypeLayout { get; private set; }
        public MapArchetypeKind Archetype { get; private set; }
        public int PlanetSeed { get; private set; }
        public float StationPhaseJitterRadians { get; private set; } = 0.06f;
        public float ExtraLaneChanceMultiplier { get; private set; } = 1f;
        public float LaneBendMultiplier { get; private set; } = 1f;
        public float MaximumFairnessRatio { get; private set; } = 2f;
        public float CentralObstacleRadius => IsArchetypeLayout && Archetype == MapArchetypeKind.OrbitalRing
            ? Mathf.Min(Bounds.Max.x - Bounds.Min.x, Bounds.Max.y - Bounds.Min.y) * 0.08f : 0f;
        public float SkeletonAngle => (uint)PlanetSeed % 4 * 45f;

        public IReadOnlyList<FieldVolume> StructuralFields { get; private set; } = Array.Empty<FieldVolume>();

        public void ConfigureStructuralFields(float mainRadius, float clearance)
        {
            if (!IsArchetypeLayout || Archetype != MapArchetypeKind.TwinPass) return;
            Vector2 middle = (Bounds.Min + Bounds.Max) * 0.5f;
            Vector3 center = new Vector3(middle.x, 0f, middle.y);
            float radius = MinLaneWidth * 0.55f;
            float offset = mainRadius + clearance + PocketThickness + radius + MinLaneWidth * 0.5f + FieldCellSize;
            Vector3 direction = Quaternion.Euler(0f, SkeletonAngle, 0f) * Vector3.forward;
            StructuralFields = new[]
            {
                new FieldVolume(center + direction * offset, radius),
                new FieldVolume(center - direction * offset, radius)
            };
        }

        public MapSizeSettings ForArchetype(PlanetMapProfileEntry profile, int playerCount)
        {
            MapSizeSettings result = (MapSizeSettings)MemberwiseClone();
            result.IsArchetypeLayout = true;
            result.Archetype = profile.Archetype.Kind;
            result.PlanetSeed = profile.GetSeed();
            float scale = profile.Archetype.BoundsMultiplier * Mathf.Sqrt(Mathf.Max(1f, playerCount / 4f));
            Vector2 center = (Bounds.Min + Bounds.Max) * 0.5f;
            Vector2 half = (Bounds.Max - Bounds.Min) * (scale * 0.5f);
            Vector2 min = center - half;
            Vector2 max = center + half;
            result.Bounds = JsonUtility.FromJson<Vector2Range>(FormattableString.Invariant(
                $"{{\"<Min>k__BackingField\":{{\"x\":{min.x},\"y\":{min.y}}},\"<Max>k__BackingField\":{{\"x\":{max.x},\"y\":{max.y}}}}}"));
            result.AdditionalZonePairCount = AdditionalZonePairCount + Mathf.Max(0, (playerCount - 3) / 2) +
                profile.Archetype.AdditionalRelayPairs + profile.AdditionalRelayPairs;
            result.HazardCount = HazardCount + (Mathf.Max(0, (playerCount - 3) / 2) +
                profile.Archetype.AdditionalHazardPairs + profile.AdditionalHazardPairs) * 2;
            if (result.Archetype == MapArchetypeKind.OrbitalRing && result.HazardCount > 0)
                result.HazardCount++;
            result.FieldThreshold = profile.Archetype.FieldThreshold;
            result.RidgeThreshold = profile.Archetype.RidgeThreshold;
            result.FieldFeatureSize *= profile.Archetype.FeatureSizeMultiplier;
            result.StationPhaseJitterRadians = profile.Archetype.StationPhaseJitterRadians;
            result.ExtraLaneChanceMultiplier = profile.Archetype.ExtraLaneChanceMultiplier;
            result.LaneBendMultiplier = profile.Archetype.LaneBendMultiplier;
            result.MaximumFairnessRatio = profile.Archetype.MaximumFairnessRatio;
            result.HazardLengthShare = new Vector2(0.16f, 0.25f);
            result.HazardHalfWidthShare = new Vector2(0.018f, 0.03f);
            return result;
        }

        [field: SerializeField] public Vector2Range Bounds { get; private set; }
        [field: SerializeField, Min(0)] public int AdditionalZonePairCount { get; private set; }
        [field: SerializeField, Min(0f)] public float AdditionalZoneMainDistance { get; private set; }
        [field: SerializeField, Min(0f)] public float AdditionalZoneEdgeBand { get; private set; } = 800f;
        [field: SerializeField, Min(0f)] public float AdditionalZoneStationDistance { get; private set; } = 1100f;
        [field: SerializeField, Min(0f)] public float StationCornerJitter { get; private set; } = 180f;
        [field: FormerlySerializedAs("<MiningPointCount>k__BackingField"),
                FormerlySerializedAs("<MiningSiteCount>k__BackingField"), SerializeField, Min(2),
                Tooltip("Mining points in the unchanged Survival ring; each has three mines and satellite pads.")]
        public int SurvivalMiningSiteCount { get; private set; }
        [field: SerializeField, Min(0)] public int HazardCount { get; private set; } = 2;
        [field: SerializeField] public Vector2 HazardLengthShare { get; private set; } = new Vector2(0.35f, 0.5f);
        [field: SerializeField] public Vector2 HazardHalfWidthShare { get; private set; } = new Vector2(0.03f, 0.065f);
        [field: SerializeField, Min(0f)] public float HazardFringeWidth { get; private set; } = 60f;
        [field: SerializeField, Min(0f)] public float EdgeMargin { get; private set; } = 1500f;
        [field: SerializeField, Min(0f), Tooltip("Minimum free gap between the asteroid pockets of two points of interest.")]
        public float PointSpacing { get; private set; }
        [field: SerializeField, Tooltip("Gap between the Survival station footprint and its mining ring (Min). " +
                                        "Skirmish home mining prefers the corner behind its station.")]
        public FloatRange HomeMiningGap { get; private set; }
        [field: SerializeField, Min(0f), Tooltip("Extra gap between the station footprint and its default reinforcement zone.")]
        public float DefaultZoneGap { get; private set; }
        [field: SerializeField, Min(0.01f)] public float PlanetScale { get; private set; } = 1f;

        [field: FormerlySerializedAs("<RoadWidth>k__BackingField"), SerializeField, Min(1f),
                Tooltip("Widest free corridor kept along a lane.")]
        public float LaneWidth { get; private set; }
        [field: SerializeField, Min(1f), Tooltip("Narrowest corridor a lane pinches to at a chokepoint; capital ships must fit.")]
        public float MinLaneWidth { get; private set; }
        [field: SerializeField, Min(1f), Tooltip("Resolution of the asteroid field raster in world units.")]
        public float FieldCellSize { get; private set; } = 20f;
        [field: SerializeField, Min(1f), Tooltip("World size of one asteroid-field noise feature.")]
        public float FieldFeatureSize { get; private set; } = 250f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Noise level above which open space becomes asteroid field.")]
        public float FieldThreshold { get; private set; } = 0.55f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Ridge noise level above which long asteroid walls form; 1 disables them.")]
        public float RidgeThreshold { get; private set; } = 0.92f;
        [field: SerializeField, Min(0f), Tooltip("Width of the asteroid pocket wrapped around every capturable zone and site.")]
        public float PocketThickness { get; private set; } = 40f;
        [field: SerializeField, Min(1f), Tooltip("Width of the gap a lane cuts through a pocket; capital ships must fit.")]
        public float PocketEntranceWidth { get; private set; } = 80f;
        [field: SerializeField, Min(0f), Tooltip("Smallest field kept, in square world units.")]
        public float MinFieldArea { get; private set; } = 3000f;
        [field: SerializeField, Min(0f), Tooltip("Visual rocks per 1000 square world units of field.")]
        public float RockDensity { get; private set; } = 1f;
    }
}
