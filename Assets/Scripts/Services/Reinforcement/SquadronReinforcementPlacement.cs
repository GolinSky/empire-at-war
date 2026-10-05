using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.StationFacing;
using EmpireAtWar.Views.Reinforcement;
using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class SquadronReinforcementPlacement : IReinforcementPlacement
    {
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly IStationFacingService _stationFacingService;

        private readonly ReinforcementPreviewFactory _previewFactory;
        private readonly ReinforcementModel _model;
        private readonly SquadronFactory _squadronFactory;
        private readonly UnitSpawnView _previewPrefab;
        private readonly PlayerId _owner;
        private readonly SquadronType _squadronType;

        public SquadronReinforcementPlacement(
            IReinforcementSpawnRule spawnRule,
            IStationFacingService stationFacingService,
            ReinforcementPreviewFactory previewFactory,
            ReinforcementModel model,
            SquadronFactory squadronFactory,
            UnitSpawnView previewPrefab,
            PlayerId owner,
            SquadronType squadronType)
        {
            _spawnRule = spawnRule;
            _stationFacingService = stationFacingService;
            _previewFactory = previewFactory;
            _model = model;
            _squadronFactory = squadronFactory;
            _previewPrefab = previewPrefab;
            _owner = owner;
            _squadronType = squadronType;
        }

        public UnitSpawnView CreatePreview() => _previewFactory.Create(_previewPrefab);

        public bool IsPositionValid(Vector3 position) => _spawnRule.IsOpen(_owner, position);

        public void Spawn(Vector3 position)
        {
            Squadron squadron = _squadronFactory.Create(_owner, _squadronType, position,
                _stationFacingService.GetRotation(_owner));
            squadron.Released += () => _model.RemoveUnitCapacity(_squadronType);
            _model.AddUnitCapacity(_squadronType);
        }
    }
}
