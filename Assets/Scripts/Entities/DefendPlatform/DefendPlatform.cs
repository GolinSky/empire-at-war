using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitDeathAnimation;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatform : MonoBehaviour, IController, IInitializable, ILateDisposable, ITickable,
        IEntityLifecycle
    {
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private Vector3 _startPosition;
        private IReadOnlyList<IMonoComponent> _monoComponents;
        private IUnitDeathAnimationData _deathAnimationData;
        private IUnitDeathAnimationService _deathAnimationService;
        private bool _isReleased;

        [Inject] private DefendPlatformModel RootModel { get; }

        public event Action OnRelease;

        public string Id => GetType().Name;

        [Inject]
        private void Construct(
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            Vector3 startPosition,
            List<IMonoComponent> monoComponents,
            IUnitDeathAnimationData deathAnimationData,
            IUnitDeathAnimationService deathAnimationService)
        {
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
            transform.position = _startPosition;
            SynchronizeComponents();
        }

        public void Tick()
        {
            SynchronizeComponents();
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

            OnRelease?.Invoke();
        }

        private void SynchronizeComponents()
        {
            _healthComponent.SetMovementState(false);
            _radarComponent.SetPosition(transform.position);
        }
    }
}
