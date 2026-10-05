using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Ship.EntityFacades
{
    public sealed class ShipTooltipFacade : IEntityTooltipFacade
    {
        private readonly FactionCatalog _factions;

        private readonly ShipType _shipType;

        public ShipTooltipFacade(FactionCatalog factions, ShipType shipType)
        {
            _shipType = shipType;
            _factions = factions;
        }

        public TooltipContent Build(List<TooltipStat> stats, string status) =>
            UnitTooltipContent.Build(_factions.GetShipFactionData(_shipType), stats, status: status);
    }
}
