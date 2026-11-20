using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class MiningFacilityReinforcementPlacement : IReinforcementPlacement
    {
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly ReinforcementPreviewFactory _previewFactory;
        private readonly PlayerFactionModel _playerFactionModel;
        private readonly MiningFacilityFactory _miningFacilityFactory;
        private readonly UnitSpawnView _previewPrefab;
        private readonly PlayerId _owner;
        private readonly MiningFacilityType _facilityType;

        public MiningFacilityReinforcementPlacement(
            IReinforcementSpawnRule spawnRule,
            ReinforcementPreviewFactory previewFactory,
            PlayerFactionModel playerFactionModel,
            MiningFacilityFactory miningFacilityFactory,
            UnitSpawnView previewPrefab,
            PlayerId owner,
            MiningFacilityType facilityType)
        {
            _spawnRule = spawnRule;
            _previewFactory = previewFactory;
            _playerFactionModel = playerFactionModel;
            _miningFacilityFactory = miningFacilityFactory;
            _previewPrefab = previewPrefab;
            _owner = owner;
            _facilityType = facilityType;
        }

        // Structures keep their fixed spawn rotation.
        public bool IsRotatable => false;

        public UnitSpawnView CreatePreview() => _previewFactory.Create(_previewPrefab);

        public Quaternion GetAutoFacing(Vector3 position) => throw new System.NotSupportedException();

        public bool IsPositionValid(Vector3 position, Quaternion rotation) => _spawnRule.CanSpawnStructure(_owner, position);

        public void Spawn(Vector3 position, Quaternion rotation)
        {
            MiningFacilityEntity facility = _miningFacilityFactory.Create(_owner, _facilityType, position);
            facility.OnRelease += () =>
                _playerFactionModel.ReleaseStructure<MiningFacilityUnitRequest>(_facilityType.ToString());
        }
    }
}
