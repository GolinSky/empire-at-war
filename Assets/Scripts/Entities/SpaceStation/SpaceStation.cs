using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitDeathAnimation;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStation : MonoBehaviour, IController, IInitializable, ILateDisposable
    {
        private FogOfWarSystem _fogOfWarSystem;
        private ILocalPlayer _localPlayer;
        private PlayerId _owner;
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private Vector3 _startPosition;
        private EntityComponentLifecycle _componentLifecycle;
        private IUnitDeathAnimationData _deathAnimationData;
        private IUnitDeathAnimationService _deathAnimationService;

        [Inject] private SpaceStationData RootModel { get; }

        public string Id => GetType().Name;

        [Inject]
        private void Construct(
            FogOfWarSystem fogOfWarSystem,
            PlayerId owner,
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            Vector3 startPosition,
            List<IMonoComponent> monoComponents,
            IUnitDeathAnimationData deathAnimationData,
            IUnitDeathAnimationService deathAnimationService,
            ILocalPlayer localPlayer)
        {
            _fogOfWarSystem = fogOfWarSystem;
            _localPlayer = localPlayer;
            _owner = owner;
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _deathAnimationData = deathAnimationData;
            _deathAnimationService = deathAnimationService;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            gameObject.name = $"{_owner}_SpaceStation";
            transform.position = _startPosition;
            _radarComponent.SetPosition(transform.position);

            // Allied stations share vision with the local player.
            if (_localPlayer.IsFriendly(_owner))
            {
                _fogOfWarSystem.RegisterVisionSource(transform, 900f);
            }
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void HandleDestroyed() => Release(true);

        private void Release(bool playDeathAnimation)
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroyed;
            if (!_componentLifecycle.Release())
            {
                return;
            }
            if (playDeathAnimation)
            {
                _deathAnimationService.Play(transform, _deathAnimationData);
            }
        }
    }
}
