using EmpireAtWar.ViewComponents.Wreck;
using UnityEngine;

namespace EmpireAtWar.Services.UnitWreck
{
    /// <summary>
    /// Look and timing of one unit's wreck. Distances are world units, times are seconds.
    /// Values are written into the wreck materials on every spawn, so edits apply to the next wreck.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitWreckData", menuName = "Data/UnitWreckData")]
    public sealed class UnitWreckData : ScriptableObject
    {
        [Tooltip("Copy of the unit view on the Ship Wreck shader. Build with Tools/Rendering/Build Wreck From Selected View.")]
        [field: SerializeField] public UnitWreckView Prefab { get; private set; }
        [Tooltip("Wrecks of this type alive at once; spawning another force-recycles the oldest.")]
        [field: SerializeField, Min(1)] public int MaxActive { get; private set; } = 32;

        [Header("Cut")]
        [Tooltip("Fewest parts the unit breaks into (picked at random between min and max, both included).")]
        [field: SerializeField, Range(WreckCutPlan.MIN_PARTS, WreckCutPlan.MAX_PARTS)] public int MinParts { get; private set; } = 2;
        [field: SerializeField, Range(WreckCutPlan.MIN_PARTS, WreckCutPlan.MAX_PARTS)] public int MaxParts { get; private set; } = 3;
        [Tooltip("Share of the bigger part at each cut: 0.5 = in half, 0.7 = 70/30.")]
        [field: SerializeField, Range(0.5f, 0.95f)] public float MinCutRatio { get; private set; } = 0.5f;
        [field: SerializeField, Range(0.5f, 0.95f)] public float MaxCutRatio { get; private set; } = 0.7f;

        [Header("Timing")]
        [Tooltip("Seconds from the swap until the wreck is gone.")]
        [field: SerializeField, Min(1f)] public float Lifetime { get; private set; } = 30f;
        [Tooltip("The last seconds of the lifetime, during which the hull dissolves.")]
        [field: SerializeField, Min(0.1f)] public float DissolveDuration { get; private set; } = 8f;
        [Tooltip("Seconds until the torn edges and interior stop glowing.")]
        [field: SerializeField, Min(0.1f)] public float GlowDuration { get; private set; } = 8f;

        [Header("Motion")]
        [Tooltip("How fast the end parts drift apart along the ship, units per second. A middle part only sinks.")]
        [field: SerializeField, Min(0f)] public float SeparationSpeed { get; private set; } = 1.5f;
        [Tooltip("How fast the parts tilt and roll, degrees per second.")]
        [field: SerializeField, Min(0f)] public float TiltSpeed { get; private set; } = 2f;
        [Tooltip("Downward drift, units per second.")]
        [field: SerializeField, Min(0f)] public float SinkSpeed { get; private set; } = 1f;

        [Header("Look")]
        [Tooltip("How far from a cut the hull glows, world units.")]
        [field: SerializeField, Min(0.01f)] public float TornEdgeWidth { get; private set; } = 4f;
        [field: SerializeField, ColorUsage(false, true)] public Color HeatColor { get; private set; } =
            new Color(4f, 1.2f, 0.2f);
        [field: SerializeField, ColorUsage(false, true)] public Color DissolveEdgeColor { get; private set; } =
            new Color(6f, 2f, 0.4f);
        [field: SerializeField, Range(0f, 0.5f)] public float DissolveEdgeWidth { get; private set; } = 0.08f;
        [Tooltip("Size of the dissolve pattern; higher = smaller holes.")]
        [field: SerializeField, Min(0.001f)] public float DissolveNoiseScale { get; private set; } = 0.05f;

        /// <summary>Farthest a vertex can travel from its rest position, used to grow the culling bounds.</summary>
        public float MaxTravel => (SeparationSpeed + SinkSpeed * 1.8f) * Lifetime;
    }
}
