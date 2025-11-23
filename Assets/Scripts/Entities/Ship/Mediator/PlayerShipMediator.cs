using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Battle;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    public interface IUnitComponent
    {
        void SetMediator(IUnitMediator mediator);
    }
    
    public interface IUnitMediator
    {
        void HandleNewEnemy(IEntity entity);
        void OnSelect(bool isActive);
    }
    
    public class PlayerShipMediator: ITickable, IMoveCommand, IUnitMediator, IInitializable, ILateDisposable, IObserver<ISelectionSubject>
    {
        private readonly IWeaponComponent _weaponComponent;
        private readonly IHealthComponent _healthComponent;
        private readonly IShipMoveComponent _shipMoveComponent;
        private readonly IRadarComponent _radarComponent;
        private readonly ISelectionService _selectionService;
        private readonly ISelectionComponent _selectionComponent;
        private readonly AttackTargetState _attackTargetState;

        private readonly StateMachine1 _stateMachine;
        private bool _isSelected;

        public PlayerShipMediator(
            IWeaponComponent weaponComponent,
            IHealthComponent healthComponent,
            IShipMoveComponent shipMoveComponent,
            IRadarComponent radarComponent,
            ISelectionService selectionService,
            ISelectionComponent selectionComponent,
            AttackTargetState attackTargetState)
        {
            _selectionService = selectionService;
            _selectionComponent = selectionComponent;
            _attackTargetState = attackTargetState;
            _weaponComponent = weaponComponent;
            _healthComponent = healthComponent;
            _shipMoveComponent = shipMoveComponent;
            _radarComponent = radarComponent;
            _stateMachine = new StateMachine1();
            _radarComponent.SetMediator(this);
            _selectionComponent.SetMediator(this);
        }
        
        public void Initialize()
        {
            _selectionService.AddObserver(this);
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
        }
        
        public void Tick()
        {
            _stateMachine.Update();
        }

        public void MoveTo(Vector2 screenPosition)
        {
            _stateMachine.ExitState();
            _shipMoveComponent.MoveToPositionOnScreen(screenPosition);
            // _moveToPointState.SetScreenCoordinates(screenPosition);
            // _shipStateMachine.ChangeState(_moveToPointState);
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

        public void UpdateState(ISelectionSubject selectionSubject)
        {
            if(!_isSelected) return;

            if (selectionSubject.UpdatedType == PlayerType.Opponent && selectionSubject.EnemySelectionContext.HasSelectable)
            {
                IHealthModelObserver healthModel = selectionSubject.EnemySelectionContext.Entity.Model
                    .GetModelObserver<IHealthModelObserver>();
                
                if (!healthModel.IsDestroyed && healthModel.HasUnits && !_attackTargetState.IsTheSameTarget(selectionSubject.EnemySelectionContext.Entity))
                {
                    _attackTargetState.SetData(selectionSubject.EnemySelectionContext.Entity); 
                    _stateMachine.SetState(_attackTargetState);
                }
            }
        }
    }
}