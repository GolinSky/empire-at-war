using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class SquadronReinforcementPlacement : IReinforcementPlacement
    {
        private readonly IReinforcementSpawnRule _spawnRule;

        private readonly ReinforcementPreviewFactory _previewFactory;
        private readonly ReinforcementModel _model;
        private readonly ISquadronSpawner _squadronSpawner;
        private readonly UnitSpawnView _previewPrefab;
        private readonly PlayerId _owner;
        private readonly SquadronType _squadronType;

        public SquadronReinforcementPlacement(
            IReinforcementSpawnRule spawnRule,
            ReinforcementPreviewFactory previewFactory,
            ReinforcementModel model,
            ISquadronSpawner squadronSpawner,
            UnitSpawnView previewPrefab,
            PlayerId owner,
            SquadronType squadronType)
        {
            _spawnRule = spawnRule;
            _previewFactory = previewFactory;
            _model = model;
            _squadronSpawner = squadronSpawner;
            _previewPrefab = previewPrefab;
            _owner = owner;
            _squadronType = squadronType;
        }

        public bool IsRotatable => true;

        public UnitSpawnView CreatePreview() => _previewFactory.Create(_previewPrefab);

        public Quaternion GetAutoFacing(Vector3 position) => _previewFactory.GetEnemyFacing(position);

        public bool IsPositionValid(Vector3 position, Quaternion rotation) => _spawnRule.IsOpen(_owner, position);

        public void Spawn(Vector3 position, Quaternion rotation)
        {
            Squadron squadron = _squadronSpawner.Spawn(_owner, _squadronType, position, rotation);
            long squadronId = squadron.EntityId;
            squadron.Released += () => _model.RemoveUnitCapacity(_squadronType, squadronId);
            _model.AddUnitCapacity(_squadronType, squadronId);
        }
    }
}
