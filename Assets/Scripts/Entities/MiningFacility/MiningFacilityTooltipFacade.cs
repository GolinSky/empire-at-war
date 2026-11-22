using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.MiningFacility
{
    public sealed class MiningFacilityTooltipFacade : IEntityTooltipFacade
    {
        private readonly IMiningFacilityModelObserver _model;
        private readonly IHealthTooltipObserver _health;

        public MiningFacilityTooltipFacade(IMiningFacilityModelObserver model, IHealthTooltipObserver health)
        {
            _model = model;
            _health = health;
        }

        public TooltipContent Build(List<TooltipStat> stats, string status)
        {
            HealthTooltipStats.AddHullAndShields(stats, _health);
            stats.Add(new TooltipStat(label: "Base income per payment", current: _model.BaseIncome));
            return new TooltipContent(title: "Mining facility", description: "Provides recurring credits to its owner.", stats: stats, status: status);
        }
    }
}
