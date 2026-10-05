using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Ship;
using EmpireAtWar.Views.Reinforcement;
using UnityEngine;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class ShipReinforcementPlacement : IReinforcementPlacement
    {
        private readonly IReinforcementSpawnRule _spawnRule;

        private readonly ReinforcementPreviewFactory _previewFactory;
        private readonly ReinforcementModel _model;
        private readonly ShipFactory _shipFactory;
        private readonly UnitSpawnView _previewPrefab;
        private readonly PlayerId _owner;
        private readonly ShipType _shipType;
        private readonly float _height;

        public ShipReinforcementPlacement(
            IReinforcementSpawnRule spawnRule,
            ReinforcementPreviewFactory previewFactory,
            ReinforcementModel model,
            ShipFactory shipFactory,
            UnitSpawnView previewPrefab,
            PlayerId owner,
            ShipType shipType,
            float height)
        {
            _spawnRule = spawnRule;
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

        public bool IsPositionValid(Vector3 position) => _spawnRule.CanSpawnShip(_owner, _shipType, position);

        public void Spawn(Vector3 position)
        {
            ShipEntity ship = _shipFactory.Create(_owner, _shipType, position);
            ship.OnRelease += _model.RemoveUnitCapacity;
            _model.AddUnitCapacity(_shipType);
        }
    }
}
