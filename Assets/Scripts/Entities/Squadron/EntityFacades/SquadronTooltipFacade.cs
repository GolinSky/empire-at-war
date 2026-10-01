using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public sealed class SquadronTooltipFacade : IEntityTooltipFacade
    {
        private readonly SquadronType _squadronType;
        private readonly FactionsData _factions;
        private readonly IHealthModelObserver _health;

        public SquadronTooltipFacade(SquadronType squadronType, FactionsData factions, IHealthModelObserver health)
        {
            _squadronType = squadronType;
            _factions = factions;
            _health = health;
        }

        public TooltipContent Build(List<TooltipStat> stats, string status)
        {
            stats.Add(new TooltipStat("Surviving fighters", _health.HardPointModels.Count(member => !member.IsDestroyed),
                _health.HardPointModels.Length));
            return UnitTooltipContent.Build(_factions.GetSquadronFactionData(_squadronType), stats, status: status);
        }
    }
}
