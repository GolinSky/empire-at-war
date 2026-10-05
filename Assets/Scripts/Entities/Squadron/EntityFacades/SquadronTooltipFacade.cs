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
        private readonly IHealthModelObserver _health;

        private readonly FactionCatalog _factions;

        private readonly SquadronType _squadronType;

        public SquadronTooltipFacade(IHealthModelObserver health, FactionCatalog factions, SquadronType squadronType)
        {
            _squadronType = squadronType;
            _factions = factions;
            _health = health;
        }

        public TooltipContent Build(List<TooltipStat> stats, string status)
        {
            stats.Add(new TooltipStat(label: "Surviving fighters", current: _health.HardPointModels.Count(member => !member.IsDestroyed),
                max: _health.HardPointModels.Length));
            return UnitTooltipContent.Build(_factions.GetSquadronFactionData(_squadronType), stats, status: status);
        }
    }
}
