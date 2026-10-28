using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Hazards
{
    [CreateAssetMenu(fileName = nameof(SpaceHazardData), menuName = "Data/Map/Space Hazard")]
    public sealed class SpaceHazardData : Data
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public HazardType HazardType { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public Color MinimapColor { get; private set; }
        [field: SerializeField] public Sprite MinimapIcon { get; private set; }
        [field: SerializeField] public bool BlocksSpecialAbilities { get; private set; } = true;
        [field: SerializeField] public bool DisablesShields { get; private set; }
        [field: SerializeField] public bool BlocksReinforcements { get; private set; } = true;
        [field: SerializeField, Min(0f)] public float ShieldRecoveryDelay { get; private set; } = 2f;
        [field: SerializeField, Min(0f)] public float ExitHysteresis { get; private set; } = 10f;
        [field: SerializeField, Min(0f)] public float PathCost { get; private set; } = 3f;
        [field: SerializeField] public Material VisualMaterial { get; private set; }
    }
}
