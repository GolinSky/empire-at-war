using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitDeathAnimation;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface ISpaceStationCommand : ICommand
    {
    }

    public class SpaceStation : MonoBehaviour, IController, ISpaceStationCommand, IInitializable, ILateDisposable,
        IEntityLifecycle
    {
        private FogOfWarSystem _fogOfWarSystem;
        private PlayerType _playerType;
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private Vector3 _startPosition;
        private IReadOnlyList<IMonoComponent> _monoComponents;
        private IUnitDeathAnimationData _deathAnimationData;
        private IUnitDeathAnimationService _deathAnimationService;
        private bool _isReleased;

        [Inject] private SpaceStationModel RootModel { get; }

        public string Id => GetType().Name;

        [Inject]
        private void Construct(
            FogOfWarSystem fogOfWarSystem,
            PlayerType playerType,
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            Vector3 startPosition,
            List<IMonoComponent> monoComponents,
            IUnitDeathAnimationData deathAnimationData,
            IUnitDeathAnimationService deathAnimationService)
        {
            _fogOfWarSystem = fogOfWarSystem;
            _playerType = playerType;
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
            _startPosition = startPosition;
            _monoComponents = monoComponents;
            _deathAnimationData = deathAnimationData;
            _deathAnimationService = deathAnimationService;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
            gameObject.name = $"{_playerType}_SpaceStation";
            transform.position = _startPosition;
            _healthComponent.SetMovementState(false);
            _radarComponent.SetPosition(transform.position);

            if (_playerType == PlayerType.Player)
            {
                _fogOfWarSystem.RegisterVisionSource(transform, 180f);
            }
        }

        public void LateDispose()
        {
            Release();
        }

        public void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            foreach (IMonoComponent component in _monoComponents)
            {
                component.Release();
            }
            _deathAnimationService.Play(transform, _deathAnimationData);
        }
    }
}
