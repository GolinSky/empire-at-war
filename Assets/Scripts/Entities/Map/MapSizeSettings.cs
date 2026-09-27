using System;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
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
        [field: SerializeField, Min(0f), Tooltip("Minimum free gap between the rings of two points of interest.")]
        public float PointSpacing { get; private set; }
        [field: SerializeField, Min(1f)] public float RoadWidth { get; private set; }
        [field: SerializeField, Min(1f)] public float ObstacleSpacing { get; private set; }
        [field: SerializeField] public FloatRange ObstacleScale { get; private set; }
        [field: SerializeField, Tooltip("Gap between the station footprint and its home mining site, Easy (Min) to hardest (Max).")]
        public FloatRange HomeMiningGap { get; private set; }
        [field: SerializeField, Min(0.01f)] public float PlanetScale { get; private set; } = 1f;
    }
}
