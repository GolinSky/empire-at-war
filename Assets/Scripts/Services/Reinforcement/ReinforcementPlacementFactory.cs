using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.StationFacing;
using EmpireAtWar.Ship;

namespace EmpireAtWar.Services.Reinforcement
{
    /// <summary>Turns a reinforcement request into the placement rules for that unit kind.</summary>
    public sealed class ReinforcementPlacementFactory
    {
        private readonly IReinforcementZonesSystem _zones;
        private readonly IShipSpawnClearance _shipSpawnClearance;
        private readonly IStationFacingService _stationFacingService;
        private readonly IAssetService _assetService;

        private readonly StructurePlacementArea _structureArea;
        private readonly ReinforcementPreviewFactory _previewFactory;
        private readonly ReinforcementModel _model;
        private readonly ReinforcementData _data;
        private readonly PlayerFactionModel _playerFactionModel;
        private readonly ShipFactory _shipFactory;
        private readonly SquadronFactory _squadronFactory;
        private readonly MiningFacilityFactory _miningFacilityFactory;
        private readonly DefendPlatformFactory _defendPlatformFactory;
        private readonly ShipsData _shipsData;
        private readonly PlayerSlot _owner;

        public ReinforcementPlacementFactory(
            IReinforcementZonesSystem zones,
            IShipSpawnClearance shipSpawnClearance,
            IStationFacingService stationFacingService,
            IAssetService assetService,
            StructurePlacementArea structureArea,
            ReinforcementPreviewFactory previewFactory,
            ReinforcementModel model,
            ReinforcementData data,
            PlayerFactionModel playerFactionModel,
            ShipFactory shipFactory,
            SquadronFactory squadronFactory,
            MiningFacilityFactory miningFacilityFactory,
            DefendPlatformFactory defendPlatformFactory,
            ShipsData shipsData,
            PlayerSlot owner)
        {
            _zones = zones;
            _shipSpawnClearance = shipSpawnClearance;
            _stationFacingService = stationFacingService;
            _assetService = assetService;
            _structureArea = structureArea;
            _previewFactory = previewFactory;
            _model = model;
            _data = data;
            _playerFactionModel = playerFactionModel;
            _shipFactory = shipFactory;
            _squadronFactory = squadronFactory;
            _miningFacilityFactory = miningFacilityFactory;
            _defendPlatformFactory = defendPlatformFactory;
            _shipsData = shipsData;
            _owner = owner;
        }

        /// <summary>Returns false when the population limit leaves no room for the unit.</summary>
        public bool TryCreate(UnitRequest request, out IReinforcementPlacement placement)
        {
            switch (request)
            {
                case ShipUnitRequest shipRequest when !_model.CanSpawnUnit(shipRequest.Key):
                case SquadronUnitRequest squadronRequest when !_model.CanSpawnUnit(squadronRequest.Key):
                    placement = null;
                    return false;
                case ShipUnitRequest shipRequest:
                    placement = new ShipReinforcementPlacement(_zones, _shipSpawnClearance, _previewFactory,
                        _model, _shipFactory, _data.GetSpawnPrefab(shipRequest.Key), _owner.Id,
                        shipRequest.Key,
                        _assetService.Load<ShipData>(_shipsData.GetShipDataPath(shipRequest.Key)).Height);
                    return true;
                case SquadronUnitRequest squadronRequest:
                    placement = new SquadronReinforcementPlacement(_zones, _stationFacingService,
                        _previewFactory, _model, _squadronFactory, _data.GetSpawnPrefab(squadronRequest.Key),
                        _owner.Id, squadronRequest.Key);
                    return true;
                case MiningFacilityUnitRequest facilityRequest:
                    placement = new MiningFacilityReinforcementPlacement(_structureArea, _previewFactory,
                        _playerFactionModel, _miningFacilityFactory, _data.GetSpawnPrefab(facilityRequest.Key),
                        _owner.Id, facilityRequest.Key);
                    return true;
                case DefendPlatformUnitRequest platformRequest:
                    placement = new DefendPlatformReinforcementPlacement(_structureArea, _previewFactory,
                        _playerFactionModel, _defendPlatformFactory, _data.GetSpawnPrefab(platformRequest.Key),
                        _owner.Id, platformRequest.Key);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }
        }
    }
}
