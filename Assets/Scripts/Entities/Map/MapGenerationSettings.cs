using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using UnityEngine;
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

        [field: SerializeField, Min(0f), Tooltip("Distance from both adjacent borders to a station pivot.")]
        public float StationEdgeDistance { get; private set; } = 125f;
        [field: SerializeField] public float StationHeight { get; private set; } = -40f;
        [field: SerializeField, Min(0f)] public float ZoneClearance { get; private set; } = 30f;
        [field: SerializeField, Range(0f, 45f), Tooltip("Largest turn of the home mining site from the map edge toward the center.")]
        public float HomeMiningAngle { get; private set; } = 20f;
        [field: SerializeField, Range(0f, 0.5f), Tooltip("Share of the map side kept free of the planet at every border.")]
        public float PlanetBorderInset { get; private set; } = 0.25f;
        [field: SerializeField, Range(0f, 1f)] public float ExtraRoadChance { get; private set; } = 0.35f;
        [field: SerializeField, Range(0f, 0.5f), Tooltip("Maximum sideways bend of a road relative to its length.")]
        public float RoadBend { get; private set; } = 0.2f;
        [field: SerializeField, Min(1f), Tooltip("Asteroid footprint diameter at scale 1.")]
        public float ObstacleFootprint { get; private set; } = 32f;

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
    }
}
