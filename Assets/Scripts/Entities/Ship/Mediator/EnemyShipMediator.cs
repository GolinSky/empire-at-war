using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.CoroutineService;
using Zenject;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    public class EnemyShipMediator : ITickable, IUnitMediator, IInitializable, ILateDisposable
    {
        private const float DELAY_TIME = 1f;

        private readonly IWeaponComponent _weaponComponent;
        private readonly IHealthComponent _healthComponent;
        private readonly IShipMoveComponent _shipMoveComponent;
        private readonly IRadarComponent _radarComponent;
        private readonly ISelectionService _selectionService;
        private readonly ICoroutineService _coroutineService;
        private readonly IMapModelObserver _mapModelObserver;

        private readonly ShipAIBrain _shipAIBrain;
        private bool _isSelected;

        public EnemyShipMediator(
            IWeaponComponent weaponComponent,
            IHealthComponent healthComponent,
            IShipMoveComponent shipMoveComponent,
            IRadarComponent radarComponent,

            ICoroutineService coroutineService,
            IMapModelObserver mapModelObserver,
            ShipAIBrain shipAIBrain)
        {
            _coroutineService = coroutineService;
            _mapModelObserver = mapModelObserver;
            _weaponComponent = weaponComponent;
            _healthComponent = healthComponent;
            _shipMoveComponent = shipMoveComponent;
            _radarComponent = radarComponent;
            _shipAIBrain = shipAIBrain;
            _radarComponent.SetMediator(this);
        }

        public void Initialize()
        {
            _coroutineService.InvokeWithDelay(() =>
            {
                _shipAIBrain.Enable(true);
            }, 11f);

        }

        public void LateDispose()
        {

        }

        public void Tick()
        {
        }

        public void HandleNewEnemy(IEntity entity)
        {
            var healthModel = entity.Model.GetModelObserver<IHealthModelObserver>();
            if (healthModel.HasUnits && entity.TryGetCommand(out IHealthCommand healthCommand))
            {
                AttackData attackData = new AttackData(healthModel, healthCommand, HardPointType.Any);
                _weaponComponent.AddTarget(attackData, AttackType.Base);
            }
        }

        public void OnSelect(bool isActive)
        {
            _isSelected = isActive;
        }
    }
}