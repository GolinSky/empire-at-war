using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.Tooltip
{
    /// <summary>
    /// Builds one <see cref="UnitSheet"/> per ship or squadron type from its live data assets, so tooltip numbers
    /// always match the unit that spawns.
    /// </summary>
    public sealed class UnitSheetCatalog : IUnitSheetCatalog
    {
        private const string SQUADRON_DATA_POSTFIX = nameof(SquadronData);

        private readonly FactionCatalog _factions;
        private readonly ShipsData _ships;
        private readonly IAssetService _assets;
        private readonly Dictionary<UnitTypeId, UnitSheet> _sheets = new Dictionary<UnitTypeId, UnitSheet>();

        public UnitSheetCatalog(FactionCatalog factions, ShipsData ships, IAssetService assets)
        {
            _factions = factions;
            _ships = ships;
            _assets = assets;
        }

        public UnitSheet Get(UnitTypeId unitType)
        {
            if (_sheets.TryGetValue(unitType, out UnitSheet sheet)) return sheet;
            sheet = unitType.IsShip ? CreateShip(unitType.ShipType) : CreateSquadron(unitType.SquadronType);
            _sheets.Add(unitType, sheet);
            return sheet;
        }

        private UnitSheet CreateShip(ShipType shipType)
        {
            ShipData data = _assets.Load<ShipData>(_ships.GetShipDataPath(shipType));
            return new UnitSheet(_factions.GetShipFactionData(shipType), FindShipFaction(shipType), data.ShipClass,
                data.Roles, data.Hull, data.Shields, data.ShieldRegenerateValue / data.ShieldRegenerateDelay, true);
        }

        private UnitSheet CreateSquadron(SquadronType squadronType)
        {
            SquadronData data = _assets.Load<SquadronData>(squadronType + SQUADRON_DATA_POSTFIX);
            return new UnitSheet(_factions.GetSquadronFactionData(squadronType), FindSquadronFaction(squadronType),
                data.ShipClass, data.Roles, data.TotalHull, data.TotalShields,
                data.ShieldRegenerateValue * data.MemberCount / data.ShieldRegenerateDelay, false);
        }

        // A unit listed by several rosters (the Tector) takes the first faction that lists it.
        private FactionType FindShipFaction(ShipType shipType)
        {
            foreach (FactionDefinition faction in _factions.Factions)
                if (faction.Ships.ContainsKey(shipType)) return faction.FactionType;
            throw new KeyNotFoundException($"No faction builds ship {shipType}.");
        }

        private FactionType FindSquadronFaction(SquadronType squadronType)
        {
            foreach (FactionDefinition faction in _factions.Factions)
                if (faction.Squadrons.ContainsKey(squadronType)) return faction.FactionType;
            throw new KeyNotFoundException($"No faction fields squadron {squadronType}.");
        }
    }
}
