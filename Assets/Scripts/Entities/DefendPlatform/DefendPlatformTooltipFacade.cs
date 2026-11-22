using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public sealed class DefendPlatformTooltipFacade : IEntityTooltipFacade
    {
        private readonly DefendPlatformCatalog _catalog;
        private readonly DefendPlatformType _type;

        private readonly IHealthTooltipObserver _health;

        public DefendPlatformTooltipFacade(DefendPlatformCatalog catalog, DefendPlatformType type, IHealthTooltipObserver health)
        {
            _catalog = catalog;
            _type = type;
            _health = health;
        }

        public TooltipContent Build(List<TooltipStat> stats, string status)
        {
            if (_type != DefendPlatformType.BattleAsteroid)
                return UnitTooltipContent.Build(_catalog.Get(_type), stats, status: status);
            HealthTooltipStats.AddHullAndShields(stats, _health);
            return new TooltipContent(title: "Battle Asteroid", description: "Defends the surrounding area.", stats: stats,
                status: status);
        }
    }
}
