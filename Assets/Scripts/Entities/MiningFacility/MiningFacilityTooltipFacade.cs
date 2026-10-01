using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.MiningFacility
{
    public sealed class MiningFacilityTooltipFacade : IEntityTooltipFacade
    {
        private readonly IMiningFacilityModelObserver _model;

        public MiningFacilityTooltipFacade(IMiningFacilityModelObserver model)
        {
            _model = model;
        }

        public TooltipContent Build(List<TooltipStat> stats, string status)
        {
            stats.Add(new TooltipStat("Base income per payment", _model.BaseIncome));
            return new TooltipContent("Mining facility", "Provides recurring credits to its owner.", stats: stats, status: status);
        }
    }
}
