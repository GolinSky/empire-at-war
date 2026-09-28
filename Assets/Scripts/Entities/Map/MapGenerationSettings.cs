using EmpireAtWar.Entities.Planet;
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
        [SerializeField] private DictionaryWrapper<PlanetType, GameObject> planetPrefabs;
        [SerializeField, Tooltip("Visual rock layers; shares are relative weights of the rock count.")]
        private DictionaryWrapper<AsteroidSize, RockLayerSettings> rockLayers;

        [field: SerializeField, Min(0f), Tooltip("Free gap between the largest station footprint and both adjacent borders.")]
        public float StationEdgeDistance { get; private set; } = 125f;
        [field: SerializeField] public float StationHeight { get; private set; } = -40f;
        [field: SerializeField, Min(0f)] public float ZoneClearance { get; private set; } = 30f;
        [field: SerializeField, Range(0f, 45f), Tooltip("Largest turn of the home mining site from the map edge toward the center.")]
        public float HomeMiningAngle { get; private set; } = 20f;
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

        public GameObject GetPlanetPrefab(PlanetType planetType)
        {
            return planetPrefabs.Dictionary[planetType];
        }

        public RockLayerSettings GetRockLayer(AsteroidSize size)
        {
            return rockLayers.Dictionary[size];
        }
    }
}
