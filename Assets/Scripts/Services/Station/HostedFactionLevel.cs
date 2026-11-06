using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Stations
{
    public sealed class HostedFactionLevel : IDisposable
    {
        private readonly PlayerFactionModel _host;
        private readonly EnemyFactionModel _ally;

        public HostedFactionLevel(PlayerFactionModel host, EnemyFactionModel ally)
        {
            _host = host;
            _ally = ally;
            _host.OnLevelUpgraded += UpdateLevel;
            try
            {
                UpdateLevel(_host.CurrentLevel);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose() => _host.OnLevelUpgraded -= UpdateLevel;

        private void UpdateLevel(int level) => _ally.CurrentLevel = level;
    }
}
