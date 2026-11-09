using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>The scout the fog lesson asks for: a squadron or a fighter, bomber or interceptor ship.</summary>
    public sealed class ScoutUnits
    {
        private readonly IAssetService _assets;
        private readonly ShipsData _shipsData;

        public ScoutUnits(IAssetService assets, ShipsData shipsData)
        {
            _assets = assets;
            _shipsData = shipsData;
        }

        public bool IsScout(UnitRequest request)
        {
            if (request is SquadronUnitRequest) return true;
            if (!(request is ShipUnitRequest ship)) return false;
            ShipClass shipClass = _assets.Load<ShipData>(_shipsData.GetShipDataPath(ship.Key)).ShipClass;
            return shipClass == ShipClass.Fighter || shipClass == ShipClass.Bomber || shipClass == ShipClass.Interceptor;
        }
    }
}
