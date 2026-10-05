using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.UnitExplosion;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStation : MonoBehaviour, IController, IInitializable, ILateDisposable
    {
        private IVisionService _visionService;
        private ISpawnBlockerService _spawnBlockerService;
        private IHealthComponent _healthComponent;
        private IUnitExplosionService _unitExplosionService;
        private IUnitWreckService _unitWreckService;

        [SerializeField] private Renderer[] explosionHullRenderers;
        private EntityComponentLifecycle _componentLifecycle;
        private GameObjectContext _context;

        private PlayerId _owner;
        private Vector3 _startPosition;
        private FactionType _factionType;

        [Inject] private SpaceStationData Data { get; }

        public string Id => GetType().Name;

        [Inject]
        private void Construct(
            IVisionService visionService,
            ISpawnBlockerService spawnBlockerService,
            IHealthComponent healthComponent,
            IUnitWreckService unitWreckService,
            IUnitExplosionService unitExplosionService,
            List<IMonoComponent> monoComponents,
            GameObjectContext context,
            PlayerId owner,
            Vector3 startPosition,
            FactionType factionType)
        {
            _visionService = visionService;
            _spawnBlockerService = spawnBlockerService;
            _owner = owner;
            _healthComponent = healthComponent;
            _startPosition = startPosition;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _unitWreckService = unitWreckService;
            _unitExplosionService = unitExplosionService;
            _context = context;
            _factionType = factionType;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            gameObject.name = $"{_owner}_SpaceStation";
            transform.position = _startPosition;
            _visionService.Register(_owner, transform, 900f);
            _spawnBlockerService.Register(_owner, transform, Data.ComponentData.SpawnBlockRadius);
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
            _visionService.Unregister(transform);
            _spawnBlockerService.Unregister(transform);
            if (playDeathAnimation)
            {
                EntityComponentData componentData = Data.ComponentData;
                _unitExplosionService.Spawn(explosionHullRenderers);
                // The explosion hides the swap: the wreck appears as the station entity is destroyed.
                if (Data.TryGetWreck(_factionType, out UnitWreckData wreck))
                {
                    _unitWreckService.Spawn(wreck, transform, _owner, componentData.DestroyDelay);
                }

                Destroy(_context.gameObject, componentData.DestroyDelay);
            }
        }
    }
}
