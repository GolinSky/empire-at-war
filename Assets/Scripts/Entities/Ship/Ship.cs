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
using EmpireAtWar.Services.Initialiaze;
using UnityEngine;
using Zenject;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.UnitDeathAnimation;

namespace EmpireAtWar.Ship
{
    public interface IShipEntity
    {
        IShipModelObserver ModelObserver { get; }
        PlayerType PlayerType { get; }
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }

        void AssignAttackTarget(IEntity target, Vector3 formationOffset);
        void AssignMoveTarget(Vector3 target);
        void HoldPosition();
    }

    public class Ship : MonoBehaviour, IController, IShipEntity, IInitializable, ILateIInitializable,
        ILateDisposable, ITickable, EmpireAtWar.Commands.Move.IMoveCommand, IUnitMediator,
        IShipMovementMediator, IEntityLifecycle
    {
        private HardPointModel _enginesUnitModel;
        private IHealthComponent _healthComponent;
        private IShipMoveComponent _shipMoveComponent;
        private IRadarComponent _radarComponent;
        private IWeaponComponent _weaponComponent;
        private ISelectionComponent _selectionComponent;
        private AttackTargetState _attackTargetState;
        private IdleState _idleState;
        private NavigateState _navigateState;
        private StateMachine1 _stateMachine;
        private ShipAIBrain _shipAIBrain;
        private IAudioShipComponent _audioShipComponent;
        private IAudioDialogShipComponent _audioDialogShipComponent;
        private IReadOnlyList<IMonoComponent> _monoComponents;
        private PlayerType _playerType;
        private bool _isReleased;
        private ILayerService _layerService;
        private IUnitDeathAnimationData _deathAnimationData;
        private IUnitDeathAnimationService _deathAnimationService;

        [Inject] private IShipService ShipService { get; }
        [Inject] private IShipData Data { get; }
        [Inject] private ShipType ShipType { get; }
        [Inject] private ShipModel RootModel { get; }

        public event Action<ShipType> OnRelease;

        public string Id => GetType().Name;
        public PlayerType PlayerType => _playerType;
        public Vector3 WorldPosition => _shipMoveComponent.CurrentPosition;
        public float NavigationRadius => _shipMoveComponent.NavigationRadius;
        IShipModelObserver IShipEntity.ModelObserver => RootModel;

        [Inject]
        private void Construct(
            IHealthComponent healthComponent,
            IShipMoveComponent shipMoveComponent,
            IRadarComponent radarComponent,
            IWeaponComponent weaponComponent,
            ISelectionComponent selectionComponent,
            AttackTargetState attackTargetState,
            IdleState idleState,
            NavigateState navigateState,
            StateMachine1 stateMachine,
            ShipAIBrain shipAIBrain,
            PlayerType playerType,
            IAudioShipComponent audioShipComponent,
            [InjectOptional] IAudioDialogShipComponent audioDialogShipComponent,
            List<IMonoComponent> monoComponents,
            ILayerService layerService,
            IUnitDeathAnimationData deathAnimationData,
            IUnitDeathAnimationService deathAnimationService)
        {
            _healthComponent = healthComponent;
            _shipMoveComponent = shipMoveComponent;
            _radarComponent = radarComponent;
            _weaponComponent = weaponComponent;
            _selectionComponent = selectionComponent;
            _attackTargetState = attackTargetState;
            _idleState = idleState;
            _navigateState = navigateState;
            _stateMachine = stateMachine;
            _shipAIBrain = shipAIBrain;
            _playerType = playerType;
            _audioShipComponent = audioShipComponent;
            _audioDialogShipComponent = audioDialogShipComponent;
            _monoComponents = monoComponents;
            _layerService = layerService;
            _deathAnimationData = deathAnimationData;
            _deathAnimationService = deathAnimationService;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
            _shipMoveComponent.SetMediator(this);
            _stateMachine.SetState(_idleState);
            ShipService.Add(this);
            _radarComponent.SetMediator(this);
            _selectionComponent.SetMediator(this);
            _audioShipComponent.PlayHyperSpace(_shipMoveComponent.HyperSpaceDuration);

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

        public void AssignAttackTarget(
            IEntity target,
            Vector3 formationOffset)
        {
            if (_playerType == PlayerType.Opponent)
            {
                _shipAIBrain.AssignAttackTarget(target, formationOffset);
            }
        }

        public void AssignMoveTarget(Vector3 target)
        {
            if (_playerType == PlayerType.Opponent)
            {
                _shipAIBrain.ClearAssignedTarget();
                _shipAIBrain.Enable(true);
                _navigateState.SetWorldDestination(target);
                _stateMachine.SetState(_navigateState);
            }
        }

        public void HoldPosition()
        {
            if (_playerType != PlayerType.Opponent)
            {
                return;
            }

            _shipAIBrain.ClearAssignedTarget();
            _shipAIBrain.Enable(false);
            _stateMachine.SetState(_idleState);
        }

        public void LateDispose()
        {
            Release(false);
        }

        public void Release()
        {
            Release(true);
        }

        private void Release(bool playDeathEffects)
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            if (playDeathEffects)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
            }
            foreach (IMonoComponent component in _monoComponents)
            {
                component.Release();
            }
            if (playDeathEffects)
            {
                _deathAnimationService.Play(transform, _deathAnimationData);
            }

            ShipService.Remove(this);

            if (_enginesUnitModel != null)
            {
                _enginesUnitModel.OnHardPointHealthChanged -= HandleEnginesData;
            }

            if (playDeathEffects && gameObject.activeInHierarchy)
            {
                OnRelease?.Invoke(ShipType);
                Instantiate(Data.DeathExplosionVfx, transform.position, Quaternion.identity);
            }
        }

        public void MoveTo(Vector2 screenPosition)
        {
            _shipAIBrain.Enable(false);
            _navigateState.SetScreenDestination(screenPosition);
            _stateMachine.SetState(_navigateState);
        }

        public void MoveTo(Vector3 worldPosition)
        {
            _shipAIBrain.Enable(false);
            _navigateState.SetWorldDestination(worldPosition);
            _stateMachine.SetState(_navigateState);
        }

        public void Attack(IEntity target, Vector3 formationOffset)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (ReferenceEquals(
                    _stateMachine.CurrentState,
                    _attackTargetState) &&
                _attackTargetState.IsTheSameTarget(
                    target,
                    formationOffset))
            {
                return;
            }

            _shipAIBrain.Enable(false);
            _attackTargetState.SetData(target, formationOffset);
            _stateMachine.SetState(_attackTargetState);
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

        public void HandleRadarContacts(IReadOnlyList<RadarContact> contacts)
        {
            _shipMoveComponent.HandleRadarContacts(contacts);
        }

        public void OnPositionChanged(Vector3 position)
        {
            _audioDialogShipComponent?.HandleMove(position);
        }

        public void OnLookAtTarget(Vector3 targetPosition)
        {
            _audioDialogShipComponent?.HandleAttack(targetPosition);
        }

        public void OnStopped()
        {
            _audioDialogShipComponent?.HandleStopped();
        }

        public void OnSelect(bool isActive)
        {
            _shipMoveComponent.HandleSelection(isActive);
            _audioDialogShipComponent?.HandleSelection(isActive);
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
