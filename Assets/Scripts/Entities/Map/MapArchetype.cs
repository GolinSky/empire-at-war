using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    [CreateAssetMenu(fileName = nameof(MapArchetype), menuName = "Data/Map Archetype")]
    public sealed class MapArchetype : ScriptableObject
    {
        [field: SerializeField] public MapArchetypeKind Kind { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField, Range(2, 8)] public int MaximumPlayers { get; private set; } = 8;
        [field: SerializeField, Range(1f, 1.25f)] public float BoundsMultiplier { get; private set; } = 1.15f;
        [field: SerializeField, Range(0f, 1f)] public float FieldThreshold { get; private set; } = 0.68f;
        [field: SerializeField, Range(0f, 1f)] public float RidgeThreshold { get; private set; } = 0.98f;
        [field: SerializeField, Min(0.1f)] public float FeatureSizeMultiplier { get; private set; } = 1f;
        [field: SerializeField, Min(0)] public int AdditionalRelayPairs { get; private set; }
        [field: SerializeField, Min(0)] public int AdditionalHazardPairs { get; private set; }
        [field: SerializeField, Range(0f, 0.5f)] public float StationPhaseJitterRadians { get; private set; } = 0.06f;
        [field: SerializeField, Range(0f, 2f)] public float ExtraLaneChanceMultiplier { get; private set; } = 1f;
        [field: SerializeField, Range(0f, 2f)] public float LaneBendMultiplier { get; private set; } = 1f;
        [field: SerializeField, Min(1f)] public float MaximumFairnessRatio { get; private set; } = 2f;
    }
}
