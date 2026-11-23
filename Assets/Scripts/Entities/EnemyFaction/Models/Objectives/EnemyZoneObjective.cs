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
        /// <summary>
        /// This AI's own armed structures (satellites, platforms) and wings in the zone. They fight beside any force
        /// sent, so a small force can hold a zone around them.
        /// </summary>
        public ForceComposition OwnSupport { get; } = new ForceComposition();
        /// <summary>
        /// Hostiles that can be at the zone before help arrives (<see cref="HelpSeconds"/>), plus the ships the hostile
        /// income buys and drops near it in that time. The garrison must outlast it (W47).
        /// </summary>
        public ForceComposition Threat { get; } = new ForceComposition();
        /// <summary>This AI's own satellites and platforms standing in the zone.</summary>
        public int OwnStructures { get; private set; }
        /// <summary>Capture sites in the zone not owned by this AI's team.</summary>
        public int OpenSites { get; private set; }
        /// <summary>The team holds the zone's relay, or this AI has a satellite or platform standing in it.</summary>
        public bool IsHeld { get; private set; }
        /// <summary>A held zone with hostiles in reach: defended before any new zone, around its satellites.</summary>
        public bool IsHeldUnderAttack => IsHeld && !Defenders.IsEmpty;

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
        /// <summary>Hostiles are inside a site this AI owns in the zone: defending it comes before any new zone.</summary>
        public bool IsThreatened { get; private set; }

        /// <summary>The capture is finished: the team owns the zone's relay (a lone site: the site).</summary>
        public bool IsOwned { get; private set; }
        /// <summary>
        /// What taking the zone is worth in credits: <see cref="WorthShare"/> of the team income over the value horizon.
        /// The objective value of <see cref="CombatMatchup.ExpectedGain"/> (W67).
        /// </summary>
        public float Value { get; private set; }
        /// <summary>
        /// Zone income as a share of the team income, plus relay access and the own platforms already standing there.
        /// A garrison may cost at most this share of the fleet (W47).
        /// </summary>
        public float WorthShare { get; private set; }
        /// <summary>Loss-exchange correction against the zone's hostiles (<see cref="EnemyTradeLedger.GetCorrection"/>); 1 = none.</summary>
        public float TradeCorrection { get; private set; } = 1f;
        /// <summary>Seconds since the team took the zone; 0 while it does not own it.</summary>
        public float HeldSeconds { get; private set; }
        /// <summary>Hostiles stand near the zone or an armada heads for it: the whole capturing force stays.</summary>
        public bool IsHostileNear { get; private set; }
        /// <summary>
        /// Seconds until help reaches the zone: the nearest own or allied group, or a reinforcement dropped nearby.
        /// </summary>
        public float HelpSeconds { get; private set; }
        public EnemyZonePlan Plan { get; private set; }
        /// <summary>Caution level from retakes, failed captures, losses, hostile spawn access, armadas and fleet rating.</summary>
        public float Caution { get; private set; }

        public void Reset(string id, bool isCentre, bool isOwnSide, float priority, float travelSeconds,
            int staticDefenders, string denyReason, bool isThreatened = false, bool isHeld = false,
            int ownStructures = 0, int openSites = 0)
        {
            OpenSites = openSites;
            IsHeld = isHeld;
            OwnStructures = ownStructures;
            Id = id;
            IsCentre = isCentre;
            IsOwnSide = isOwnSide;
            Priority = priority;
            TravelSeconds = travelSeconds;
            StaticDefenders = staticDefenders;
            DenyReason = denyReason;
            IsThreatened = isThreatened;
            IsOwned = false;
            Value = 0f;
            WorthShare = 0f;
            TradeCorrection = 1f;
            HeldSeconds = 0f;
            IsHostileNear = false;
            HelpSeconds = 0f;
            Plan = EnemyZonePlan.TakeAndHold;
            Caution = 0f;
            Threat.Clear();
        }

        public void SetWorth(bool isOwned, float value, float worthShare)
        {
            IsOwned = isOwned;
            Value = value;
            WorthShare = worthShare;
        }

        /// <summary>Hold inputs of the garrison rule; <see cref="Threat"/> is filled beside it.</summary>
        public void SetHold(float heldSeconds, bool isHostileNear, float helpSeconds, float tradeCorrection)
        {
            HeldSeconds = heldSeconds;
            IsHostileNear = isHostileNear;
            HelpSeconds = helpSeconds;
            TradeCorrection = tradeCorrection;
        }

        /// <summary>Sets the plan; <see cref="EnemyZonePlan.Skip"/> also denies the zone.</summary>
        public void SetPlan(EnemyZonePlan plan, float caution)
        {
            Plan = plan;
            Caution = caution;
            if (plan == EnemyZonePlan.Skip && DenyReason == null) DenyReason = "caution";
        }
    }
}
