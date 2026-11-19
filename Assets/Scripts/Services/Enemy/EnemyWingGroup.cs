using System.Collections.Generic;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Presenters.ReinforcementZones;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// A standing AI wing group: its members, its role and the zone it works this pass. Group wings capture; they
    /// are never pulled off to escort. A lost wing is replaced by the next free wing.
    /// </summary>
    public sealed class EnemyWingGroup
    {
        public EnemyWingRole Role { get; }
        public List<ISquadron> Members { get; } = new List<ISquadron>();
        /// <summary>The zone the group works; null when it has none (no centre on the map, or no outer zone left).</summary>
        public ReinforcementZonePresenter Zone { get; set; }

        public EnemyWingGroup(EnemyWingRole role) => Role = role;
    }
}
