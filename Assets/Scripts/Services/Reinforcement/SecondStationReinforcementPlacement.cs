using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class SecondStationReinforcementPlacement : IReinforcementPlacement
    {
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly ReinforcementPreviewFactory _previewFactory;
        private readonly PlayerFactionModel _playerFactionModel;
        private readonly SpaceStationFactory _stationFactory;
        private readonly UnitSpawnView _previewPrefab;
        private readonly PlayerId _owner;
        private readonly FactionType _faction;

        public SecondStationReinforcementPlacement(
            IReinforcementSpawnRule spawnRule,
            ReinforcementPreviewFactory previewFactory,
            PlayerFactionModel playerFactionModel,
            SpaceStationFactory stationFactory,
            UnitSpawnView previewPrefab,
            PlayerId owner,
            FactionType faction)
        {
            _spawnRule = spawnRule;
            _previewFactory = previewFactory;
            _playerFactionModel = playerFactionModel;
            _stationFactory = stationFactory;
            _previewPrefab = previewPrefab;
            _owner = owner;
            _faction = faction;
        }

        public UnitSpawnView CreatePreview() => _previewFactory.Create(_previewPrefab, Quaternion.identity);

        public bool IsPositionValid(Vector3 position) => _spawnRule.CanSpawnStructure(_owner, position);

        public void Spawn(Vector3 position)
        {
            SpaceStationEntity station = _stationFactory.CreateSecond(_owner, _faction, position);
            station.Destroyed += () =>
                _playerFactionModel.ReleaseStructure<SecondStationUnitRequest>(SecondStationUnitRequest.REQUEST_ID);
        }
    }
}
