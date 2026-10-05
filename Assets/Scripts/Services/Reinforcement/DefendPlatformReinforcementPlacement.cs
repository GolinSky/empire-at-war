using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Views.Reinforcement;
using UnityEngine;
using DefendPlatformEntity = EmpireAtWar.Entities.DefendPlatform.DefendPlatform;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class DefendPlatformReinforcementPlacement : IReinforcementPlacement
    {
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly ReinforcementPreviewFactory _previewFactory;
        private readonly PlayerFactionModel _playerFactionModel;
        private readonly DefendPlatformFactory _defendPlatformFactory;
        private readonly UnitSpawnView _previewPrefab;
        private readonly PlayerId _owner;
        private readonly DefendPlatformType _platformType;

        public DefendPlatformReinforcementPlacement(
            IReinforcementSpawnRule spawnRule,
            ReinforcementPreviewFactory previewFactory,
            PlayerFactionModel playerFactionModel,
            DefendPlatformFactory defendPlatformFactory,
            UnitSpawnView previewPrefab,
            PlayerId owner,
            DefendPlatformType platformType)
        {
            _spawnRule = spawnRule;
            _previewFactory = previewFactory;
            _playerFactionModel = playerFactionModel;
            _defendPlatformFactory = defendPlatformFactory;
            _previewPrefab = previewPrefab;
            _owner = owner;
            _platformType = platformType;
        }

        public UnitSpawnView CreatePreview() => _previewFactory.Create(_previewPrefab);

        public bool IsPositionValid(Vector3 position) => _spawnRule.CanSpawnStructure(_owner, position);

        public void Spawn(Vector3 position)
        {
            DefendPlatformEntity platform = _defendPlatformFactory.Create(_owner, _platformType, position);
            platform.OnRelease += () =>
                _playerFactionModel.ReleaseStructure<DefendPlatformUnitRequest>(_platformType.ToString());
        }
    }
}
