using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatform : MonoBehaviour, IController, IInitializable, ILateDisposable, ITickable,
        IEntityLifecycle
    {
        private IHealthComponent _healthComponent;
        private IRadarComponent _radarComponent;
        private IReadOnlyList<IMonoComponent> _monoComponents;
        private bool _isReleased;

        [Inject] private DefendPlatformModel RootModel { get; }

        public string Id => GetType().Name;

        [Inject]
        private void Construct(
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            List<IMonoComponent> monoComponents)
        {
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
            _monoComponents = monoComponents;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
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
        }

        private void SynchronizeComponents()
        {
            _healthComponent.SetMovementState(false);
            _radarComponent.SetPosition(transform.position);
        }
    }
}
