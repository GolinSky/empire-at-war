using EmpireAtWar.Components.Movement;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatformController : Controller<DefendPlatformModel>, IInitializable, ITickable
    {
        private readonly LazyInject<DefendPlatformView> _view;
        private readonly IHealthComponent _healthComponent;
        private readonly IRadarComponent _radarComponent;

        public DefendPlatformController(
            DefendPlatformModel model,
            LazyInject<DefendPlatformView> view,
            IHealthComponent healthComponent,
            IRadarComponent radarComponent) : base(model)
        {
            _view = view;
            _healthComponent = healthComponent;
            _radarComponent = radarComponent;
        }

        public void Initialize()
        {
            SynchronizeComponents();
        }

        public void Tick()
        {
            SynchronizeComponents();
        }

        private void SynchronizeComponents()
        {
            _healthComponent.SetMovementState(false);
            _radarComponent.SetPosition(_view.Value.transform.position);
        }
    }
}
