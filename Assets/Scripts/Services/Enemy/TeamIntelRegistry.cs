using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Scene-wide intel per team, so allied AIs read and write the same hostile records.</summary>
    public sealed class TeamIntelRegistry
    {
        private readonly Dictionary<TeamId, HostileIntelModel> _teams = new Dictionary<TeamId, HostileIntelModel>();

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
