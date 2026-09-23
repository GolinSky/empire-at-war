using EmpireAtWar.Components.Radar;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Components.Ship.Movement
{
    public sealed class ShipVisionRegistration : IInitializable, ILateDisposable
    {
        private readonly FogOfWarSystem _fogOfWarSystem;
        private readonly IRadarModelObserver _radarModel;
        private readonly PlayerType _playerType;
        private readonly Transform _viewTransform;

        public ShipVisionRegistration(
            FogOfWarSystem fogOfWarSystem,
            IRadarModelObserver radarModel,
            PlayerType playerType,
            [Inject(Id = EntityBindType.ViewTransform)] Transform viewTransform)
        {
            _fogOfWarSystem = fogOfWarSystem;
            _radarModel = radarModel;
            _playerType = playerType;
            _viewTransform = viewTransform;
        }

        public void Initialize()
        {
            if (_playerType == PlayerType.Player)
            {
                _fogOfWarSystem.RegisterVisionSource(
                    _viewTransform, _radarModel.Range);
            }
        }

        public void LateDispose()
        {
            if (_playerType == PlayerType.Player)
            {
                _fogOfWarSystem.UnregisterVisionSource(_viewTransform);
            }
        }
    }
}
