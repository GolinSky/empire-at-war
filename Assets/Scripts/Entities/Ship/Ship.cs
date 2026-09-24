using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
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
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Timing;
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
        float NavigationSpeed { get; }
        long EntityId { get; }
        ShipOrderType CurrentOrder { get; }
    }

    public class Ship : MonoBehaviour, IController, IShipEntity, IInitializable,
        ILateDisposable, ITickable, IUnitMediator,
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
        private AttackMoveState _attackMoveState;
        private GuardState _guardState;
        private HuntState _huntState;
        private ShipOrderModel _orderModel;
        private LazyInject<IEntity> _entity;
        private StateMachine1 _stateMachine;
        private ShipAIBrain _shipAIBrain;
        private IAudioShipComponent _audioShipComponent;
        private IAudioDialogShipComponent _audioDialogShipComponent;
        private EntityComponentLifecycle _componentLifecycle;
        private PlayerType _playerType;
        private bool _isReleased;
        private ILayerService _layerService;
        private ICameraService _cameraService;
        private IUnitDeathAnimationData _deathAnimationData;
        private IUnitDeathAnimationService _deathAnimationService;

        [Inject] private IShipService ShipService { get; }
        [Inject] private IShipData Data { get; }
        [Inject] private ShipType ShipType { get; }
        [Inject] private ShipData RootModel { get; }

        public event Action<ShipType> OnRelease;

        public string Id => GetType().Name;
        public PlayerType PlayerType => _playerType;
        public Vector3 WorldPosition => _shipMoveComponent.CurrentPosition;
        public float NavigationRadius => _shipMoveComponent.NavigationRadius;
        public float NavigationSpeed => _shipMoveComponent.NavigationSpeed;
        public long EntityId => _entity.Value.Id;
        public ShipOrderType CurrentOrder => _orderModel.Current;
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
            AttackMoveState attackMoveState,
            GuardState guardState,
            HuntState huntState,
            ShipOrderModel orderModel,
            LazyInject<IEntity> entity,
            StateMachine1 stateMachine,
            ShipAIBrain shipAIBrain,
            PlayerType playerType,
            IAudioShipComponent audioShipComponent,
            [InjectOptional] IAudioDialogShipComponent audioDialogShipComponent,
            List<IMonoComponent> monoComponents,
            ILayerService layerService,
            ICameraService cameraService,
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
            _attackMoveState = attackMoveState;
            _guardState = guardState;
            _huntState = huntState;
            _orderModel = orderModel;
            _entity = entity;
            _stateMachine = stateMachine;
            _shipAIBrain = shipAIBrain;
            _playerType = playerType;
            _audioShipComponent = audioShipComponent;
            _audioDialogShipComponent = audioDialogShipComponent;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _layerService = layerService;
            _cameraService = cameraService;
            _deathAnimationData = deathAnimationData;
            _deathAnimationService = deathAnimationService;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
            foreach (HardPointModel hardPointModel in _healthComponent.HealthModelObserver.HardPointModels)
            {
                if (hardPointModel.HardPointType == HardPointType.Engines)
                {
                    _enginesUnitModel = hardPointModel;
                    _enginesUnitModel.OnHardPointHealthChanged += HandleEnginesData;
                    break;
                }
            }

            _shipMoveComponent.SetMediator(this);
            _stateMachine.SetState(_idleState);
            ShipService.Add(this);
            _radarComponent.SetMediator(this);
            _selectionComponent.SetMediator(this);
            _audioShipComponent.PlayHyperSpace(_shipMoveComponent.HyperSpaceDuration);

            SynchronizeComponents();
        }

        public void Tick()
        {
            if (_isReleased)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ShipTick.Auto())
            {
#endif
            _stateMachine.Update();
            CompleteNavigation();
            if (_orderModel.Current == ShipOrderType.Guard &&
                _stateMachine.CurrentState == _guardState && _guardState.IsComplete)
                Stop();
            if (_orderModel.Current == ShipOrderType.Hunt &&
                _stateMachine.CurrentState == _huntState && _huntState.IsComplete)
                Stop();
            if (_orderModel.Current == ShipOrderType.Attack &&
                _stateMachine.CurrentState == _idleState)
                _orderModel.Clear();
            SynchronizeComponents();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
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
            _isReleased = true;
            _orderModel.Clear();
            _shipAIBrain.Enable(false);
            if (!_componentLifecycle.Release())
            {
                return;
            }
            if (playDeathEffects)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
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
            MoveTo(_cameraService.GetWorldPoint(screenPosition,
                _shipMoveComponent.CurrentPosition));
        }

        public void MoveTo(Vector3 worldPosition)
        {
            FormationPoint destination = ToPoint(worldPosition);
            if (_orderModel.Matches(ShipOrderType.Move, destination)) return;
            _orderModel.Replace(ShipOrderType.Move, destination);
            _shipAIBrain.Enable(_playerType == PlayerType.Opponent);
            _navigateState.SetWorldDestination(worldPosition);
            if (!_shipAIBrain.IsFleeing) _stateMachine.SetState(_navigateState);
        }

        public void Attack(IEntity target, Vector3 formationOffset)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            FormationPoint offset = ToPoint(formationOffset);
            if (_orderModel.Matches(ShipOrderType.Attack, target: target, offset: offset)) return;
            _orderModel.Replace(ShipOrderType.Attack, target: target, offset: offset);
            _shipAIBrain.Enable(_playerType == PlayerType.Opponent);
            _attackTargetState.SetData(target, formationOffset);
            if (!_shipAIBrain.IsFleeing) _stateMachine.SetState(_attackTargetState);
        }

        public void AttackMoveTo(Vector3 destination)
        {
            FormationPoint point = ToPoint(destination);
            if (_orderModel.Matches(ShipOrderType.AttackMove, point)) return;
            _orderModel.Replace(ShipOrderType.AttackMove, point);
            _shipAIBrain.Enable(_playerType == PlayerType.Opponent);
            _attackMoveState.SetDestination(destination);
            if (!_shipAIBrain.IsFleeing) _stateMachine.SetState(_attackMoveState);
        }

        public void Guard(IEntity friendly, Vector3 offset)
        {
            FormationPoint formationOffset = ToPoint(offset);
            if (_orderModel.Matches(ShipOrderType.Guard, target: friendly,
                    offset: formationOffset)) return;
            _orderModel.Replace(ShipOrderType.Guard, target: friendly,
                offset: formationOffset);
            _shipAIBrain.Enable(_playerType == PlayerType.Opponent);
            _guardState.SetData(friendly, offset);
            if (!_shipAIBrain.IsFleeing) _stateMachine.SetState(_guardState);
        }

        public void MoveAlong(IReadOnlyList<Vector3> waypoints)
        {
            if (waypoints.Count == 0) return;
            List<FormationPoint> points = new List<FormationPoint>(waypoints.Count);
            foreach (Vector3 waypoint in waypoints) points.Add(ToPoint(waypoint));
            bool same = _orderModel.Current == ShipOrderType.WaypointMove &&
                        _orderModel.Waypoints.Count == points.Count;
            for (int i = 0; same && i < points.Count; i++)
            {
                float x = _orderModel.Waypoints[i].X - points[i].X;
                float z = _orderModel.Waypoints[i].Z - points[i].Z;
                same = x * x + z * z <= 0.01f;
            }
            if (same) return;
            _orderModel.Replace(ShipOrderType.WaypointMove, points[0], waypoints: points);
            _shipAIBrain.Enable(_playerType == PlayerType.Opponent);
            _navigateState.SetWorldDestination(waypoints[0]);
            if (!_shipAIBrain.IsFleeing) _stateMachine.SetState(_navigateState);
        }

        public void Hunt()
        {
            if (_orderModel.Current == ShipOrderType.Hunt) return;
            _orderModel.Replace(ShipOrderType.Hunt);
            _shipAIBrain.Enable(_playerType == PlayerType.Opponent);
            if (!_shipAIBrain.IsFleeing) _stateMachine.SetState(_huntState);
        }

        public void Retreat(Vector3 destination)
        {
            FormationPoint point = ToPoint(destination);
            if (_orderModel.Matches(ShipOrderType.Retreat, point)) return;
            _orderModel.Replace(ShipOrderType.Retreat, point);
            _shipAIBrain.Enable(_playerType == PlayerType.Opponent);
            _weaponComponent.ResetTarget();
            _navigateState.SetWorldDestination(destination);
            if (!_shipAIBrain.IsFleeing) _stateMachine.SetState(_navigateState);
        }

        public void Stop()
        {
            _orderModel.Clear();
            _weaponComponent.ResetTarget();
            _shipMoveComponent.Stop();
            _shipAIBrain.Enable(_playerType == PlayerType.Opponent);
            _stateMachine.SetState(_idleState);
        }

        public void ResumeOrder()
        {
            switch (_orderModel.Current)
            {
                case ShipOrderType.Move:
                case ShipOrderType.WaypointMove:
                case ShipOrderType.Retreat:
                    _navigateState.SetWorldDestination(ToVector(_orderModel.Destination));
                    _stateMachine.SetState(_navigateState);
                    break;
                case ShipOrderType.Attack:
                    if (_orderModel.Target == null ||
                        _orderModel.Target.HealthModel.IsDestroyed)
                    {
                        Stop();
                        break;
                    }
                    _attackTargetState.SetData(_orderModel.Target, ToVector(_orderModel.Offset));
                    _stateMachine.SetState(_attackTargetState);
                    break;
                case ShipOrderType.AttackMove:
                    _attackMoveState.SetDestination(ToVector(_orderModel.Destination));
                    _stateMachine.SetState(_attackMoveState);
                    break;
                case ShipOrderType.Guard:
                    if (_orderModel.Target == null ||
                        _orderModel.Target.HealthModel.IsDestroyed)
                    {
                        Stop();
                        break;
                    }
                    _guardState.SetData(_orderModel.Target, ToVector(_orderModel.Offset));
                    _stateMachine.SetState(_guardState);
                    break;
                case ShipOrderType.Hunt:
                    _stateMachine.SetState(_huntState);
                    break;
                default:
                    _stateMachine.SetState(_idleState);
                    break;
            }
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

        private void CompleteNavigation()
        {
            bool navigating = _stateMachine.CurrentState == _navigateState ||
                              _stateMachine.CurrentState == _attackMoveState &&
                              !_attackMoveState.IsEngaging;
            if (!navigating || _shipMoveComponent.IsMoving || _shipMoveComponent.IsBlocked)
            {
                return;
            }
            if (_orderModel.Current == ShipOrderType.WaypointMove &&
                _orderModel.AdvanceWaypoint(out FormationPoint next))
            {
                _navigateState.SetWorldDestination(ToVector(next));
                _stateMachine.SetState(_navigateState);
                return;
            }
            _orderModel.Clear();
            _stateMachine.SetState(_idleState);
        }

        private static FormationPoint ToPoint(Vector3 value) =>
            new FormationPoint(value.x, value.z);

        private static Vector3 ToVector(FormationPoint value) =>
            new Vector3(value.X, 0f, value.Z);

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
