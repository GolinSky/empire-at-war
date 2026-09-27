using System;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Entities.Map
{
    [Serializable]
    public sealed class MapSizeSettings
    {
        [field: SerializeField] public Vector2Range Bounds { get; private set; }
        [field: SerializeField, Min(0)] public int CapturableZoneCount { get; private set; }
        [field: SerializeField, Min(2), Tooltip("Includes the home mining site of each side.")]
        public int MiningSiteCount { get; private set; }
        [field: SerializeField, Min(0)] public int BattleSiteCount { get; private set; }
        [field: SerializeField, Min(0f), Tooltip("Minimum free gap between the asteroid pockets of two points of interest.")]
        public float PointSpacing { get; private set; }
        [field: SerializeField, Tooltip("Gap between the station footprint and its home mining site, Easy (Min) to hardest (Max).")]
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
