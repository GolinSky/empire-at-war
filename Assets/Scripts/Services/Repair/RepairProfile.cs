using UnityEngine;

namespace EmpireAtWar.Services.Repair
{
    /// <summary>Repair capability of one carrier type; a unit data asset references it to enable repairing.</summary>
    [CreateAssetMenu(fileName = nameof(RepairProfile), menuName = "Data/Repair Profile")]
    public sealed class RepairProfile : ScriptableObject
    {
        [field: SerializeField, Min(1f), Tooltip("Flat (top-down) world units from the repairer to the target centre; height is ignored.")]
        public float Radius { get; private set; } = 400f;

        [field: SerializeField, Min(0.1f), Tooltip("Seconds between repair ticks.")]
        public float Interval { get; private set; } = 1f;

        [field: SerializeField, Range(0.01f, 100f), Tooltip("Percent of maximum hull and of each live hardpoint restored per tick.")]
        public float PercentPerTick { get; private set; } = 0.5f;

        [field: SerializeField, Min(1)]
        public int MaxTargets { get; private set; } = 10;

        [field: SerializeField, Min(1), Tooltip("Repairers that may work on one target at the same time.")]
        public int MaxHealersPerTarget { get; private set; } = 2;

        public float FractionPerTick => PercentPerTick / 100f;
    }
}
