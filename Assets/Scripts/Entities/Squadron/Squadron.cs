using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.Squadrons
{
    /// <summary>
    /// Squadron-level order handling. The player commands the squadron; each fighter flies on its own
    /// through <see cref="SquadronPilot"/>, and weapons fire whenever a nose lines up with an enemy.
    /// </summary>
    public sealed class Squadron : MonoBehaviour, IController, ISquadron, IInitializable, ITickable,
        ILateDisposable
    {
        private const float RELEASED_CONTEXT_LIFETIME = 6f;

        private ISquadronFlightComponent _flight;
        private IHealthComponent _health;
        private IRadarComponent _radar;
        private IWeaponComponent _weapon;
        private UnitOrderModel _orders;
        private SquadronPilot _pilot;
        private SquadronTargetSelector _targetSelector;
        private IAttackDataFactory _attackDataFactory;
        private ICameraService _cameraService;
        private ILayerService _layerService;
        private UnitOrderSettings _orderSettings;
        private GameObjectContext _context;
        private LazyInject<IEntity> _entity;
        private EntityComponentLifecycle _componentLifecycle;
        private IEntity _engaged;
        private float _huntRetargetTimer;
        private bool _isReleased;

        [Inject] private SquadronData Data { get; }

        public event Action Released;

        public string Id => GetType().Name;
        public Vector3 WorldPosition => _flight.Centroid;
        public float NavigationRadius => Data.NavigationRadius;

        [Inject]
        private void Construct(ISquadronFlightComponent flight, IHealthComponent health, IRadarComponent radar,
            IWeaponComponent weapon, UnitOrderModel orders, SquadronPilot pilot,
            SquadronTargetSelector targetSelector, IAttackDataFactory attackDataFactory,
            ICameraService cameraService, ILayerService layerService, UnitOrderSettings orderSettings,
            GameObjectContext context, LazyInject<IEntity> entity, List<IMonoComponent> monoComponents)
        {
            _flight = flight;
            _health = health;
            _radar = radar;
            _weapon = weapon;
            _orders = orders;
            _pilot = pilot;
            _targetSelector = targetSelector;
            _attackDataFactory = attackDataFactory;
            _cameraService = cameraService;
            _layerService = layerService;
            _orderSettings = orderSettings;
            _context = context;
            _entity = entity;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
        }

        public IModel GetModel() => Data;

        public void Initialize()
        {
            _health.HealthModelObserver.OnDestroy += HandleDestroyed;
            _radar.Enemies.ItemAdded += HandleEnemyAdded;
            // A hangar issues its guard order right after creation, before the fighters have spawned.
            if (_orders.Current == UnitOrderType.Guard) EscortGuarded();
            else _pilot.Loiter(_flight.Centroid + _flight.Heading * Data.LoiterRadius);
            _radar.SetPosition(_flight.Centroid);
        }

        public void Tick()
        {
            if (_isReleased)
            {
                return;
            }

            UpdateOrder();
            _pilot.Tick(Time.deltaTime);
            _flight.Step(Time.deltaTime);
            _radar.SetPosition(_flight.Centroid);
        }

        public void LateDispose() => Release(false);

        private void HandleDestroyed() => Release(true);

        public void MoveTo(Vector2 screenPosition) =>
            MoveTo(_cameraService.GetWorldPoint(screenPosition, _flight.Centroid));

        public void MoveTo(Vector3 worldPosition)
        {
            if (_orders.Matches(UnitOrderType.Move, ToPoint(worldPosition))) return;
            _orders.Replace(UnitOrderType.Move, ToPoint(worldPosition));
            Disengage();
            _pilot.FlyTo(worldPosition);
        }

        public void Attack(IEntity target, Vector3 formationOffset)
        {
            // Fighters swarm the target, so a formation offset has no meaning for squadrons.
            if (!SquadronTargetSelector.IsAlive(target) ||
                _orders.Matches(UnitOrderType.Attack, target: target)) return;
            _orders.Replace(UnitOrderType.Attack, target: target);
            Engage(target);
        }

        public void AttackMoveTo(Vector3 worldPosition)
        {
            if (_orders.Matches(UnitOrderType.AttackMove, ToPoint(worldPosition))) return;
            _orders.Replace(UnitOrderType.AttackMove, ToPoint(worldPosition));
            Disengage();
            _pilot.FlyTo(worldPosition);
        }

        public void Guard(IEntity friendly, Vector3 offset)
        {
            if (!SquadronTargetSelector.IsAlive(friendly) ||
                _orders.Matches(UnitOrderType.Guard, target: friendly)) return;
            _orders.Replace(UnitOrderType.Guard, target: friendly);
            Disengage();
            EscortGuarded();
        }

        public void MoveAlong(IReadOnlyList<Vector3> waypoints)
        {
            if (waypoints.Count == 0) return;
            List<FormationPoint> points = new List<FormationPoint>(waypoints.Count);
            foreach (Vector3 waypoint in waypoints) points.Add(ToPoint(waypoint));
            if (_orders.MatchesWaypoints(points)) return;
            _orders.Replace(UnitOrderType.WaypointMove, points[0], waypoints: points);
            Disengage();
            _pilot.FlyTo(waypoints[0]);
        }

        public void Hunt()
        {
            if (_orders.Current == UnitOrderType.Hunt) return;
            _orders.Replace(UnitOrderType.Hunt);
            _huntRetargetTimer = 0f;
        }

        public void Retreat(Vector3 destination)
        {
            if (_orders.Matches(UnitOrderType.Retreat, ToPoint(destination))) return;
            _orders.Replace(UnitOrderType.Retreat, ToPoint(destination));
            Disengage();
            _pilot.FlyTo(destination);
        }

        public void Stop()
        {
            _orders.Clear();
            Disengage();
            _pilot.Loiter(_flight.Centroid);
        }

        private void HandleEnemyAdded(ObservableList<IEntity> sender, ListChangedEventArgs<IEntity> args)
        {
            IEntity entity = args.item;
            if (entity.HealthModel.HasUnits && entity.TryGetFacade(out IHealthFacade healthFacade))
            {
                _weapon.AddTarget(new AttackData(entity.HealthModel, healthFacade, HardPointType.Any),
                    AttackType.Base);
            }
        }

        private void UpdateOrder()
        {
            switch (_orders.Current)
            {
                case UnitOrderType.Attack:
                    if (!SquadronTargetSelector.IsAlive(_orders.Target)) Stop();
                    break;
                case UnitOrderType.Hunt:
                    UpdateHunt();
                    break;
                case UnitOrderType.Guard:
                    if (!SquadronTargetSelector.IsAlive(_orders.Target)) Stop();
                    else DefendArea(_orders.Target.GetFacade<IEntityTransformFacade>().Transform.position);
                    break;
                case UnitOrderType.AttackMove:
                    DefendArea(_flight.Centroid);
                    if (_engaged == null && _pilot.HasArrived) _orders.Clear();
                    break;
                case UnitOrderType.Move:
                case UnitOrderType.Retreat:
                    if (_pilot.HasArrived) _orders.Clear();
                    break;
                case UnitOrderType.WaypointMove:
                    if (!_pilot.HasArrived) break;
                    if (_orders.AdvanceWaypoint(out FormationPoint next)) _pilot.FlyTo(ToVector(next));
                    else _orders.Clear();
                    break;
                default:
                    DefendArea(_pilot.LoiterCenter);
                    break;
            }
        }

        private void UpdateHunt()
        {
            _huntRetargetTimer -= Time.deltaTime;
            if (SquadronTargetSelector.IsAlive(_engaged) && _huntRetargetTimer > 0f) return;
            _huntRetargetTimer = _orderSettings.HuntRetargetInterval;
            IEntity target = _targetSelector.SelectAnywhere(_flight.Centroid);
            if (target != null) Engage(target);
            else if (_engaged != null) Stop();
        }

        /// <summary>Engages radar contacts near <paramref name="center"/> and resumes the order once they are gone.</summary>
        private void DefendArea(Vector3 center)
        {
            if (SquadronTargetSelector.IsAlive(_engaged)) return;
            IEntity enemy = _targetSelector.SelectNear(_radar.Enemies, center, Data.GuardRadius);
            if (enemy != null)
            {
                Engage(enemy);
                return;
            }

            if (_engaged == null) return;
            Disengage();
            ResumeCourse();
        }

        private void ResumeCourse()
        {
            switch (_orders.Current)
            {
                case UnitOrderType.Guard:
                    EscortGuarded();
                    break;
                case UnitOrderType.AttackMove:
                    _pilot.FlyTo(ToVector(_orders.Destination));
                    break;
                default:
                    _pilot.Loiter(_flight.Centroid);
                    break;
            }
        }

        private void EscortGuarded()
        {
            IEntity friendly = _orders.Target;
            _pilot.Escort(friendly.GetFacade<IEntityTransformFacade>().Transform, SquadronPilot.GetRadius(friendly));
        }

        private void Engage(IEntity target)
        {
            if (ReferenceEquals(_engaged, target)) return;
            _engaged = target;
            _weapon.ResetTarget();
            _weapon.AddTarget(_attackDataFactory.ConstructData(target), AttackType.MainTarget);
            _pilot.Engage(target);
        }

        private void Disengage()
        {
            if (_engaged == null) return;
            _engaged = null;
            _weapon.ResetTarget();
        }

        private void Release(bool destroyed)
        {
            _health.HealthModelObserver.OnDestroy -= HandleDestroyed;
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            _radar.Enemies.ItemAdded -= HandleEnemyAdded;
            _orders.Clear();
            _engaged = null;
            _componentLifecycle.Release();
            if (destroyed)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
                Destroy(_context.gameObject, RELEASED_CONTEXT_LIFETIME);
            }

            Released?.Invoke();
        }

        private static FormationPoint ToPoint(Vector3 value) => new FormationPoint(value.x, value.z);
        private static Vector3 ToVector(FormationPoint value) => new Vector3(value.X, 0f, value.Z);
    }
}
