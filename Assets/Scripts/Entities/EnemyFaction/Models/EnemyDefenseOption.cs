using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public readonly struct EnemyDefenseOption
    {
        public SiteFacilityType Type { get; }
        public float Cost { get; }
        public UnitCombatProfile Profile { get; }
        public float Range { get; }

        public EnemyDefenseOption(SiteFacilityType type, float cost, UnitCombatProfile profile, float range)
        {
            Type = type;
            Cost = cost;
            Profile = profile;
            Range = range;
        }
    }
}
