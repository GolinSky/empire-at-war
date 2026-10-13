using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Services.UnitExplosion;
using UnityEngine;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Lifecycle;
using VContainer;
using LifetimeScope = VContainer.Unity.LifetimeScope;
using VContainer.Internal;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatform : EntityView, IController, IInitializable, ILateDisposable
    {
        private IHealthComponent _healthComponent;
        private IUnitExplosionService _unitExplosionService;
        private IUnitWreckService _unitWreckService;
        private ILayerService _layerService;

        [SerializeField] private Renderer[] explosionHullRenderers;
        private EntityComponentLifecycle _componentLifecycle;
        private LifetimeScope _scope;

        private Vector3 _startPosition;
        private PlayerId _owner;

        public event Action OnRelease;

        private DefendPlatformData Data { get; set; }

        public override string Id => GetType().Name;

        [Inject]
        private void Construct(
            IHealthComponent healthComponent,
            IUnitWreckService unitWreckService,
            IUnitExplosionService unitExplosionService,
            ILayerService layerService,
            ContainerLocal<IReadOnlyList<IMonoComponent>> monoComponents,
            LifetimeScope scope,
            Vector3 startPosition,
            PlayerId owner, DefendPlatformData data)
        {
            _healthComponent = healthComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents.Value);
            _unitWreckService = unitWreckService;
            _unitExplosionService = unitExplosionService;
            _scope = scope;
            _owner = owner;
            _layerService = layerService;
            Data = data;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            transform.position = _startPosition;
        }

        public void LateDispose()
        {
            Release(false);
        }

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
                OnRelease?.Invoke();
                EntityComponentData componentData = Data.ComponentData;
                _unitExplosionService.Spawn(explosionHullRenderers);
                // The explosion hides the swap: the wreck appears as the platform entity is destroyed.
                if (Data.Wreck != null)
                {
                    _unitWreckService.Spawn(Data.Wreck, transform, _owner, componentData.DestroyDelay);
                }

                Destroy(_scope.gameObject, componentData.DestroyDelay);
            }
        }

        private void HandleDestroyed() => Release(true);
    }
}
