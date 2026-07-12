using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.CoroutineService;
using EmpireAtWar.Services.Initialiaze;
using UnityEngine;
using Zenject;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Ship
{
    public interface IShipEntity
    {
        IShipModelObserver ModelObserver { get; }
        PlayerType PlayerType { get; }

        void AssignAttackTarget(IEntity target);
    }

    public class Ship : MonoBehaviour, IController, IShipEntity, IInitializable, ILateIInitializable,
        ILateDisposable, ITickable, EmpireAtWar.Commands.Move.IMoveCommand, IUnitMediator,
        IObserver<ISelectionSubject>, IEntityLifecycle
    {
        private HardPointModel _enginesUnitModel;
        private IHealthComponent _healthComponent;
        private IShipMoveComponent _shipMoveComponent;
        private IRadarComponent _radarComponent;
        private IWeaponComponent _weaponComponent;
        private ISelectionComponent _selectionComponent;
        private ISelectionService _selectionService;
        private AttackTargetState _attackTargetState;
        private IdleState _idleState;
        private StateMachine1 _stateMachine;
        private ShipAIBrain _shipAIBrain;
        private ICoroutineService _coroutineService;
        private IAudioShipComponent _audioShipComponent;
        private IAudioDialogShipComponent _audioDialogShipComponent;
        private IReadOnlyList<IMonoComponent> _monoComponents;
        private PlayerType _playerType;
        private bool _isSelected;
        private bool _isReleased;

        [Inject] private IShipService ShipService { get; }
        [Inject] private IShipData Data { get; }
        [Inject] private ShipType ShipType { get; }
        [Inject] private ShipModel RootModel { get; }

        public event Action<ShipType> OnRelease;

        public string Id => GetType().Name;
        public PlayerType PlayerType => _playerType;
        IShipModelObserver IShipEntity.ModelObserver => RootModel;

        [Inject]
        private void Construct(
            IHealthComponent healthComponent,
            IShipMoveComponent shipMoveComponent,
            IRadarComponent radarComponent,
            IWeaponComponent weaponComponent,
            ISelectionComponent selectionComponent,
            ISelectionService selectionService,
            AttackTargetState attackTargetState,
            IdleState idleState,
            StateMachine1 stateMachine,
            ShipAIBrain shipAIBrain,
            PlayerType playerType,
            ICoroutineService coroutineService,
            IAudioShipComponent audioShipComponent,
            [InjectOptional] IAudioDialogShipComponent audioDialogShipComponent,
            List<IMonoComponent> monoComponents)
        {
            _healthComponent = healthComponent;
            _shipMoveComponent = shipMoveComponent;
            _radarComponent = radarComponent;
            _weaponComponent = weaponComponent;
            _selectionComponent = selectionComponent;
            _selectionService = selectionService;
            _attackTargetState = attackTargetState;
            _idleState = idleState;
            _stateMachine = stateMachine;
            _shipAIBrain = shipAIBrain;
            _playerType = playerType;
            _coroutineService = coroutineService;
            _audioShipComponent = audioShipComponent;
            _audioDialogShipComponent = audioDialogShipComponent;
            _monoComponents = monoComponents;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
            _stateMachine.SetState(_idleState);
            ShipService.Add(this);
            _radarComponent.SetMediator(this);
            _selectionComponent.SetMediator(this);
            _selectionService.AddObserver(this);
            _audioShipComponent.PlayHyperSpace(_shipMoveComponent.HyperSpaceDuration);

            if (_audioDialogShipComponent != null)
            {
                _shipMoveComponent.TargetPositionChanged += _audioDialogShipComponent.HandleMove;
                _shipMoveComponent.LookAtTargetChanged += _audioDialogShipComponent.HandleAttack;
                _shipMoveComponent.Stopped += _audioDialogShipComponent.HandleStopped;
            }

            if (_playerType == PlayerType.Opponent)
            {
                _coroutineService.InvokeWithDelay(
                    () => _shipAIBrain.Enable(true),
                    _shipMoveComponent.HyperSpaceDuration * 2);
            }

            SynchronizeComponents();
        }

        public void LateInitialize()
        {
            foreach (HardPointModel hardPointModel in _healthComponent.HealthModelObserver.HardPointModels)
            {
                if (hardPointModel.HardPointType == HardPointType.Engines)
                {
                    _enginesUnitModel = hardPointModel;
                    break;
                }
            }

            if (_enginesUnitModel != null)
            {
                _enginesUnitModel.OnHardPointHealthChanged += HandleEnginesData;
            }
        }

        public void Tick()
        {
            _stateMachine.Update();
            SynchronizeComponents();
        }

        public void AssignAttackTarget(IEntity target)
        {
            if (_playerType == PlayerType.Opponent)
            {
                _shipAIBrain.AssignAttackTarget(target);
            }
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

            ShipService.Remove(this);
            _selectionService.RemoveObserver(this);

            if (_audioDialogShipComponent != null)
            {
                _shipMoveComponent.TargetPositionChanged -= _audioDialogShipComponent.HandleMove;
                _shipMoveComponent.LookAtTargetChanged -= _audioDialogShipComponent.HandleAttack;
                _shipMoveComponent.Stopped -= _audioDialogShipComponent.HandleStopped;
            }

            if (_enginesUnitModel != null)
            {
                _enginesUnitModel.OnHardPointHealthChanged -= HandleEnginesData;
            }

            if (gameObject.activeInHierarchy)
            {
                OnRelease?.Invoke(ShipType);
                Instantiate(Data.DeathExplosionVfx, transform.position, Quaternion.identity);
            }
        }

        public void MoveTo(Vector2 screenPosition)
        {
            _shipAIBrain.Enable(false);
            _stateMachine.ExitState();
            _shipMoveComponent.MoveToPositionOnScreen(screenPosition);
        }

        public void HandleNewEnemy(IEntity entity)
        {
            IHealthModelObserver healthModel = entity.HealthModel;
            if (healthModel.HasUnits && entity.TryGetCommand(out IHealthCommand healthCommand))
            {
                _weaponComponent.AddTarget(
                    new AttackData(healthModel, healthCommand, HardPointType.Any),
                    AttackType.Base);
            }

            _audioShipComponent.HandleEnemyDetected();
            _audioDialogShipComponent?.HandleEnemyDetected();
        }

        public void OnSelect(bool isActive)
        {
            _isSelected = isActive;
            _shipMoveComponent.HandleSelection(isActive);
            _audioDialogShipComponent?.HandleSelection(isActive);
        }

        public void UpdateState(ISelectionSubject selectionSubject)
        {
            if (!_isSelected || selectionSubject.UpdatedType != PlayerType.Opponent ||
                !selectionSubject.EnemySelectionContext.HasSelectable)
            {
                return;
            }

            IEntity entity = selectionSubject.EnemySelectionContext.Entity;
            IHealthModelObserver healthModel = entity.HealthModel;
            if (!healthModel.IsDestroyed && healthModel.HasUnits && !_attackTargetState.IsTheSameTarget(entity))
            {
                _shipAIBrain.Enable(false);
                _attackTargetState.SetData(entity);
                _stateMachine.SetState(_attackTargetState);
            }
        }

        private void SynchronizeComponents()
        {
            _healthComponent.SetMovementState(_shipMoveComponent.IsMoving);
            _radarComponent.SetPosition(_shipMoveComponent.CurrentPosition);
        }

        private void HandleEnginesData()
        {
            if (_enginesUnitModel.IsDestroyed)
            {
                _shipMoveComponent.ApplyMoveCoefficient(Data.MinMoveCoefficient);
            }
        }
    }
}
