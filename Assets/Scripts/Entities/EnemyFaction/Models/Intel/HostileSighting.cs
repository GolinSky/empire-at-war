using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Intel
{
    /// <summary>What a team last saw of one hostile unit, and when.</summary>
    public readonly struct HostileSighting
    {
        public long EntityId { get; }
        public UnitTypeId UnitTypeId { get; }
        public PlayerId Owner { get; }
        public FormationPoint Position { get; }
        public float Hull { get; }
        public float Shields { get; }

        /// <summary>Share of the unit's weapons that were still firing (0..1).</summary>
        public float OffenseScale { get; }

        /// <summary>Game time in seconds when the unit was seen.</summary>
        public float SeenAt { get; }

        public HostileSighting(
            long entityId,
            UnitTypeId unitTypeId,
            PlayerId owner,
            FormationPoint position,
            float hull,
            float shields,
            float offenseScale,
            float seenAt)
        {
            EntityId = entityId;
            UnitTypeId = unitTypeId;
            Owner = owner;
            Position = position;
            Hull = hull;
            Shields = shields;
            OffenseScale = offenseScale;
            SeenAt = seenAt;
        }
    }
}
