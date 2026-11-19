using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Objectives
{
    /// <summary>
    /// One zone the AI may take: a reinforcement zone with its pads, mines and hostile structures, or a lone site
    /// outside every zone. Defenders include armed hostile structures, so satellites and platforms count as force.
    /// </summary>
    public sealed class EnemyZoneObjective
    {
        /// <summary>Known hostile units and armed structures that fight for the zone; reused between collections.</summary>
        public ForceComposition Defenders { get; } = new ForceComposition();

        public string Id { get; private set; }
        public bool IsCentre { get; private set; }
        /// <summary>The nearest station is allied: the zone lies on this team's side of the map.</summary>
        public bool IsOwnSide { get; private set; }
        /// <summary>
        /// The zone's best site: capture value minus travel cost, in travel-distance units. Higher is better.
        /// </summary>
        public float Priority { get; private set; }
        public float TravelSeconds { get; private set; }
        public int StaticDefenders { get; private set; }
        public string DenyReason { get; private set; }
        public bool IsDenied => DenyReason != null;

        public void Reset(string id, bool isCentre, bool isOwnSide, float priority, float travelSeconds,
            int staticDefenders, string denyReason)
        {
            Id = id;
            IsCentre = isCentre;
            IsOwnSide = isOwnSide;
            Priority = priority;
            TravelSeconds = travelSeconds;
            StaticDefenders = staticDefenders;
            DenyReason = denyReason;
        }
    }
}
