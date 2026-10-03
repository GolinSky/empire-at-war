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
using Zenject;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatform : MonoBehaviour, IController, IInitializable, ILateDisposable
    {
        [SerializeField] private Renderer[] explosionHullRenderers;

        private IHealthComponent _healthComponent;
        private Vector3 _startPosition;
        private EntityComponentLifecycle _componentLifecycle;
        private IUnitExplosionService _explosionService;
        private IUnitWreckService _wreckService;
        private GameObjectContext _context;
        private PlayerId _owner;
        private ILayerService _layerService;

        [Inject] private DefendPlatformData Data { get; }

        public event Action OnRelease;

        public string Id => GetType().Name;

        [Inject]
        private void Construct(
            IHealthComponent healthComponent,
            Vector3 startPosition,
            List<IMonoComponent> monoComponents,
            IUnitWreckService wreckService,
            IUnitExplosionService explosionService,
            GameObjectContext context,
            PlayerId owner,
            ILayerService layerService)
        {
            _healthComponent = healthComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _wreckService = wreckService;
            _explosionService = explosionService;
            _context = context;
            _owner = owner;
            _layerService = layerService;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            transform.position = _startPosition;
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
                _explosionService.Spawn(explosionHullRenderers);
                // The explosion hides the swap: the wreck appears as the platform entity is destroyed.
                if (Data.Wreck != null)
                {
                    _wreckService.Spawn(Data.Wreck, transform, _owner, componentData.DestroyDelay);
                }

                Destroy(_context.gameObject, componentData.DestroyDelay);
            }
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void HandleDestroyed() => Release(true);
    }
}
