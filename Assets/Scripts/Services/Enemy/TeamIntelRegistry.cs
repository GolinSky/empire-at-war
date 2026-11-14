using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Scene-wide intel per team, so allied AIs read and write the same hostile records.</summary>
    public sealed class TeamIntelRegistry
    {
        private readonly Dictionary<TeamId, HostileIntelModel> _teams = new Dictionary<TeamId, HostileIntelModel>();
        private readonly Dictionary<TeamId, EnemyPatrolMemory> _patrols = new Dictionary<TeamId, EnemyPatrolMemory>();
        public void Clear()
        {
            _teams.Clear();
            _patrols.Clear();
        }

        public EnemyPatrolMemory GetPatrol(TeamId team)
        {
            if (!_patrols.TryGetValue(team, out EnemyPatrolMemory memory))
            {
                memory = new EnemyPatrolMemory();
                _patrols.Add(team, memory);
            }
            return memory;
        }

        public HostileIntelModel Get(TeamId team)
        {
            if (!_teams.TryGetValue(team, out HostileIntelModel intel))
            {
                intel = new HostileIntelModel();
                _teams.Add(team, intel);
            }

            return intel;
        }
    }
}
