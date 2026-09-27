using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitDeathAnimation;
using UnityEngine;
using EmpireAtWar.Services.Layer;
using Zenject;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatform : MonoBehaviour, IController, IInitializable, ILateDisposable, ITickable
    {
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private Vector3 _startPosition;
        private EntityComponentLifecycle _componentLifecycle;
        private IUnitDeathAnimationData _deathAnimationData;
        private IUnitDeathAnimationService _deathAnimationService;
        private ILayerService _layerService;

        [Inject] private DefendPlatformData RootModel { get; }

        public event Action OnRelease;

        public string Id => GetType().Name;

        [Inject]
        private void Construct(
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            Vector3 startPosition,
            List<IMonoComponent> monoComponents,
            IUnitDeathAnimationData deathAnimationData,
            IUnitDeathAnimationService deathAnimationService,
            ILayerService layerService)
        {
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _deathAnimationData = deathAnimationData;
            _deathAnimationService = deathAnimationService;
            _layerService = layerService;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            transform.position = _startPosition;
            SynchronizeComponents();
        }

        public void Tick()
        {
            SynchronizeComponents();
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void HandleDestroyed() => Release(true);

        private void Release(bool playDeathEffects)
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroyed;
            if (!_componentLifecycle.Release())
            {
                return;
            }
            if (playDeathEffects)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
                _deathAnimationService.Play(transform, _deathAnimationData);
            }

            if (playDeathEffects)
            {
                OnRelease?.Invoke();
            }
        }

        private void SynchronizeComponents()
        {
            _radarComponent.SetPosition(transform.position);
        }
    }
}
