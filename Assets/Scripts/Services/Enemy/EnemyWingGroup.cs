using System.Collections.Generic;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Presenters.ReinforcementZones;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// A standing AI wing group: its members, its role and the zone it works. Group wings capture; they are never
    /// pulled off to escort. A lost wing is replaced by the next free wing. An outer group keeps its zone until the
    /// zone is taken or the group falls below half of its peak size.
    /// </summary>
    public sealed class EnemyWingGroup
    {
        public EnemyWingRole Role { get; }
        public List<ISquadron> Members { get; } = new List<ISquadron>();
        /// <summary>The zone the group works; null when it has none (no centre on the map, or no outer zone left).</summary>
        public ReinforcementZone Zone { get; set; }
        /// <summary>The outer zone the group was formed for; it fills the group's quota. Null for the centre group.</summary>
        public ReinforcementZone HomeZone { get; }
        /// <summary>Most capturing (non-bomber) wings the group held since it last chose its zone.</summary>
        public int Peak { get; set; }
        /// <summary>Times its wings died inside the zone it works; cleared when it changes zone.</summary>
        public List<float> ZoneDeaths { get; } = new List<float>();
        /// <summary>A zone a lone wing left after dying there twice; skipped until <see cref="AbandonedUntil"/>.</summary>
        public ReinforcementZone AbandonedZone { get; set; }
        public float AbandonedUntil { get; set; }

        public EnemyWingGroup(EnemyWingRole role, ReinforcementZone homeZone = null)
        {
            Role = role;
            HomeZone = homeZone;
            Zone = homeZone;
        }
    }
}
