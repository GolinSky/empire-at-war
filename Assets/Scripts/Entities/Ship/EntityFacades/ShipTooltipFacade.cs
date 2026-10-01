using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Ship.EntityFacades
{
    public sealed class ShipTooltipFacade : IEntityTooltipFacade
    {
        private readonly ShipType _shipType;
        private readonly FactionsData _factions;

        public ShipTooltipFacade(ShipType shipType, FactionsData factions)
        {
            _shipType = shipType;
            _factions = factions;
        }

        public TooltipContent Build(List<TooltipStat> stats, string status) =>
            UnitTooltipContent.Build(_factions.GetShipFactionData(_shipType), stats, status: status);
    }
}
