using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Entities.Units.Matchups;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    /// <summary>
    /// Tooltips for unit types shown in the local player's menus (build roster, queue, reserves, selection groups):
    /// durability rows with the local player's research bonus, then the caller's context rows.
    /// </summary>
    public sealed class UnitRosterTooltips
    {
        private readonly IUnitSheetCatalog _sheets;
        private readonly MatchupRules _matchups;
        private readonly IPlayerRegistry _players;
        private readonly ILocalPlayer _localPlayer;

        public UnitRosterTooltips(IUnitSheetCatalog sheets, MatchupRules matchups, IPlayerRegistry players,
            ILocalPlayer localPlayer)
        {
            _sheets = sheets;
            _matchups = matchups;
            _players = players;
            _localPlayer = localPlayer;
        }

        public TooltipContent Build(UnitTypeId unitType, IEnumerable<TooltipStat> context,
            IEnumerable<TooltipRequirement> requirements = null, string status = "")
        {
            UnitSheet sheet = _sheets.Get(unitType);
            var stats = new List<TooltipStat>();
            UnitStatRows.Add(stats, sheet, _players.GetResearch(_localPlayer.Id));
            stats.AddRange(context);
            return UnitTooltipContent.Build(sheet, _matchups, stats, requirements, status);
        }

        /// <summary>Ship and squadron requests get the unit format; every other roster item keeps its own text.</summary>
        public TooltipContent Build(UnitRequest request, IEnumerable<TooltipStat> context,
            IEnumerable<TooltipRequirement> requirements = null, string status = "") => request switch
        {
            ShipUnitRequest ship => Build(UnitTypeId.Ship(ship.Key), context, requirements, status),
            SquadronUnitRequest squadron => Build(UnitTypeId.Squadron(squadron.Key), context, requirements, status),
            _ => UnitTooltipContent.Build(request.FactionData, context, requirements, status)
        };
    }
}
