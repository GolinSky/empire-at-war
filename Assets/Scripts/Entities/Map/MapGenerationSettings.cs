using EmpireAtWar.Entities.Planet;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.Map
{
    [CreateAssetMenu(fileName = nameof(MapGenerationSettings), menuName = "Data/Map Generation Settings")]
    public sealed class MapGenerationSettings : ScriptableObject
    {
        [SerializeField] private DictionaryWrapper<MapSize, MapSizeSettings> sizes;
        [SerializeField, Tooltip("XZ footprint radius about each station's pivot, including its model offset.")]
        private DictionaryWrapper<FactionType, float> stationRadii;
        [SerializeField] private DictionaryWrapper<PlanetType, PlanetScope> planetPrefabs;
        [SerializeField, Tooltip("Visual rock layers; shares are relative weights of the rock count.")]
        private DictionaryWrapper<AsteroidSize, RockLayerSettings> rockLayers;
        [SerializeField] private SpaceHazardData[] hazards = System.Array.Empty<SpaceHazardData>();

        public IReadOnlyList<SpaceHazardData> Hazards => hazards;

        [field: SerializeField, Tooltip("World height of an asteroid field's underside. Sits above the hulls of the " +
                                        "Deep and Lowest ship tiers, so only those pass beneath a field.")]
        public float FieldFloor { get; private set; } = -150f;
        [field: SerializeField, Tooltip("World height of an asteroid field's top. Sits above every ship hull.")]
        public float FieldCeiling { get; private set; } = 110f;
        [field: SerializeField] public float HazardFloor { get; private set; } = -650f;
        [field: SerializeField] public float HazardCeiling { get; private set; } = 180f;
        [field: SerializeField, Min(0f), Tooltip("Free gap between each station footprint and both adjacent borders.")]
        public float StationEdgeDistance { get; private set; } = 420f;
        [field: SerializeField] public float StationHeight { get; private set; } = -40f;
        [field: SerializeField, Min(0f)] public float ZoneClearance { get; private set; } = 30f;
        [field: SerializeField, Min(0f)] public float SatellitePadOffset { get; private set; } = 160f;
        [field: SerializeField, Min(0f), Tooltip("Placement radius of a satellite pad. Its capture range comes from " +
                                                 "the satellite prefab and may overlap neighbouring sites.")]
        public float SatelliteFootprintRadius { get; private set; } = 60f;
        [field: SerializeField, Range(0.1f, 0.5f), Tooltip("Half-size of the main relay region as a share of the map side.")]
        public float MainZoneRegionShare { get; private set; } = 0.25f;
        [field: SerializeField, Min(1)] public int MainZoneMiningCount { get; private set; } = 4;
        [field: SerializeField, Min(1)] public int MainZoneSatelliteCount { get; private set; } = 6;
        [field: SerializeField, Min(1)] public int AdditionalZoneSatelliteCount { get; private set; } = 2;
        [field: SerializeField, Min(0f)] public float MainZoneFairnessTolerance { get; private set; } = 300f;
        [field: SerializeField, Range(0f, 0.5f), Tooltip("Share of the map side kept free of the planet at every border.")]
        public float PlanetBorderInset { get; private set; } = 0.25f;
        [field: FormerlySerializedAs("<ExtraRoadChance>k__BackingField"), SerializeField, Range(0f, 1f)]
        public float ExtraLaneChance { get; private set; } = 0.35f;
        [field: FormerlySerializedAs("<RoadBend>k__BackingField"), SerializeField, Range(0f, 0.5f),
                Tooltip("Maximum sideways bend of a lane relative to its length.")]
        public float LaneBend { get; private set; } = 0.2f;

        public MapSizeSettings GetSize(MapSize mapSize)
        {
            return sizes.Dictionary[mapSize];
        }

        public float GetStationRadius(FactionType factionType)
        {
            return stationRadii.Dictionary[factionType];
        }

        public PlanetScope GetPlanetPrefab(PlanetType planetType)
        {
            return planetPrefabs.Dictionary[planetType];
        }

        public RockLayerSettings GetRockLayer(AsteroidSize size)
        {
            return rockLayers.Dictionary[size];
        }
    }
}
