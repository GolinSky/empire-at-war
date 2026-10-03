using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Ship;
using EmpireAtWar.Views.Reinforcement;
using UnityEngine;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class ShipReinforcementPlacement : IReinforcementPlacement
    {
        private readonly IReinforcementZonesSystem _zones;
        private readonly IShipSpawnClearance _clearance;

        private readonly ReinforcementPreviewFactory _previewFactory;
        private readonly ReinforcementModel _model;
        private readonly ShipFactory _shipFactory;
        private readonly UnitSpawnView _previewPrefab;
        private readonly PlayerId _owner;
        private readonly ShipType _shipType;
        private readonly float _height;

        public ShipReinforcementPlacement(
            IReinforcementZonesSystem zones,
            IShipSpawnClearance clearance,
            ReinforcementPreviewFactory previewFactory,
            ReinforcementModel model,
            ShipFactory shipFactory,
            UnitSpawnView previewPrefab,
            PlayerId owner,
            ShipType shipType,
            float height)
        {
            _zones = zones;
            _clearance = clearance;
            _previewFactory = previewFactory;
            _model = model;
            _shipFactory = shipFactory;
            _previewPrefab = previewPrefab;
            _owner = owner;
            _shipType = shipType;
            _height = height;
        }

        public UnitSpawnView CreatePreview()
        {
            UnitSpawnView preview = _previewFactory.Create(_previewPrefab);
            // The preview hovers where the ship will actually fly.
            preview.SetHeight(_height);
            return preview;
        }

        public bool IsPositionValid(Vector3 position)
        {
            return _zones.IsPositionInAlliedZone(_owner, position) &&
                   _clearance.IsClear(_owner, _shipType, position);
        }

        public void Spawn(Vector3 position)
        {
            ShipEntity ship = _shipFactory.Create(_owner, _shipType, position);
            ship.OnRelease += _model.RemoveUnitCapacity;
            _model.AddUnitCapacity(_shipType);
        }
    }
}
