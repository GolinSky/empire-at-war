using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface ISpaceStationCommand : ICommand
    {
    }

    public class SpaceStation : View<ISpaceStationModelObserver>, IController, ISpaceStationCommand
    {
        private FogOfWarSystem _fogOfWarSystem;
        private PlayerType _playerType;
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private Vector3 _startPosition;

        [Inject] private SpaceStationModel RootModel { get; }

        public string Id => GetType().Name;

        [Inject]
        private void Construct(
            FogOfWarSystem fogOfWarSystem,
            PlayerType playerType,
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            Vector3 startPosition)
        {
            _fogOfWarSystem = fogOfWarSystem;
            _playerType = playerType;
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
            _startPosition = startPosition;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        protected override void OnInitialize()
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

        protected override void OnDispose()
        {
        }
    }
}
