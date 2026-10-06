using static EmpireAtWar.Utils.FormationConversion;
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
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Utils;
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
        private IWeaponFireEvents _weaponFireEvents;
        private IShipSfxService _shipSfxService;
        private IAttackDataFactory _attackDataFactory;
        private ICameraService _cameraService;
        private ILayerService _layerService;
        private ShipAbilityService _abilities;
        private IShipAbilityFacade _abilityCaster;
        private IEntity _engaged;

        private UnitOrderModel _orders;
        private SquadronPilot _pilot;
        private SquadronTargetSelector _targetSelector;
        private UnitOrderSettings _orderSettings;
        private GameObjectContext _context;
        private LazyInject<IEntity> _entity;
        private EntityComponentLifecycle _componentLifecycle;
        private SFoilsModel _sFoils;
        private List<EmpireAtWar.ViewComponents.Squadrons.ISFoilsView> _sFoilsViews;

        private ShipAbilityId _pendingAbilityId;

        private float _huntRetargetTimer;
        private float _pendingAbilityRange;

        private bool _isReleased;

        public event Action Released;

        [Inject] private SquadronData Data { get; }

        public string Id => GetType().Name;
        public Vector3 WorldPosition => _flight.Centroid;
        public float NavigationRadius => Data.NavigationRadius;
        public UnitOrderType CurrentOrder => _orders.Current;

        [Inject]
        private void Construct(ISquadronFlightComponent flight, IHealthComponent health, IRadarComponent radar,
            IWeaponComponent weapon, IAttackDataFactory attackDataFactory, ICameraService cameraService,
            ILayerService layerService, UnitOrderModel orders,
            SquadronPilot pilot, SquadronTargetSelector targetSelector, UnitOrderSettings orderSettings,
            GameObjectContext context, LazyInject<IEntity> entity, List<IMonoComponent> monoComponents,
            IWeaponFireEvents weaponFireEvents, IShipSfxService shipSfxService,
            SFoilsModel sFoils, List<EmpireAtWar.ViewComponents.Squadrons.ISFoilsView> sFoilsViews,
            ShipAbilityService abilities, IShipAbilityFacade abilityCaster)
        {
            _flight = flight;
            _health = health;
            _radar = radar;
            _weapon = weapon;
            _weaponFireEvents = weaponFireEvents;
            _shipSfxService = shipSfxService;
            _sFoils = sFoils;
            _sFoilsViews = sFoilsViews;
            _orders = orders;
            _pilot = pilot;
            _targetSelector = targetSelector;
            _attackDataFactory = attackDataFactory;
            _cameraService = cameraService;
            _layerService = layerService;
            _abilities = abilities;
            _abilityCaster = abilityCaster;
            _orderSettings = orderSettings;
            _context = context;
            _entity = entity;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
        }

        public void Initialize()
        {
            _health.HealthModelObserver.OnDestroy += HandleDestroyed;
            _radar.Enemies.ItemAdded += HandleEnemyAdded;
            _weaponFireEvents.ShotEmitted += HandleShotEmitted;
            _sFoils.Changed += HandleSFoilsChanged;
            foreach (var view in _sFoilsViews) view.SetClosed(_sFoils.IsClosed, true);
            // A hangar issues its guard order right after creation, before the fighters have spawned.
            if (_orders.Current == UnitOrderType.Guard) EscortGuarded();
            else _pilot.Loiter(_flight.Centroid + _flight.Heading * Data.LoiterRadius);
        }

        public void LateDispose() => Release(false);

        public void Tick()
        {
            if (_isReleased)
            {
                return;
            }

            UpdateOrder();
            _pilot.Tick(Time.deltaTime);
            _flight.Step(Time.deltaTime);
        }

        private void HandleDestroyed() => Release(true);

        private void HandleSFoilsChanged()
        {
            foreach (var view in _sFoilsViews) view.SetClosed(_sFoils.IsClosed, false);
        }

        private void HandleShotEmitted(WeaponProfile profile, Transform muzzle) =>
            _shipSfxService.TryPlayWeaponShot(_entity.Value, profile, muzzle);

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
            if (!SquadronTargetSelector.IsAlive(target) || target.IsCloaked() ||
                _orders.Matches(UnitOrderType.Attack, target: target)) return;
            _orders.Replace(UnitOrderType.Attack, target: target);
            Engage(target);
        }

        public void CastAbility(ShipAbilityId id, IEntity target, float range)
        {
            _pendingAbilityId = id;
            _pendingAbilityRange = range;
            _orders.Replace(UnitOrderType.UseAbility, target: target);
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
                _weapon.AddTarget(new AttackData(entity.HealthModel, healthFacade, HardPointType.Any, entity),
                    AttackType.Base);
            }
        }

        private void UpdateOrder()
        {
            switch (_orders.Current)
            {
                case UnitOrderType.Attack:
                    if (!SquadronTargetSelector.IsAlive(_orders.Target) || _orders.Target.IsCloaked()) Stop();
                    break;
                case UnitOrderType.Hunt:
                    UpdateHunt();
                    break;
                case UnitOrderType.UseAbility:
                    UpdateAbilityCast();
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

        private void UpdateAbilityCast()
        {
            IEntity target = _orders.Target;
            if (!SquadronTargetSelector.IsAlive(target) || target.IsCloaked())
            {
                Stop();
                return;
            }

            Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            if (PlanarGeometry.Distance(_abilityCaster.WorldPosition, targetPosition) > _pendingAbilityRange) return;
            // The squadron keeps engaging the target after the cast.
            _orders.Replace(UnitOrderType.Attack, target: target);
            _abilities.TryActivate(_abilityCaster, _pendingAbilityId, target);
        }

        private void UpdateHunt()
        {
            _huntRetargetTimer -= Time.deltaTime;
            if (SquadronTargetSelector.IsAlive(_engaged) && !_engaged.IsCloaked() && _huntRetargetTimer > 0f) return;
            _huntRetargetTimer = _orderSettings.HuntRetargetInterval;
            IEntity target = _targetSelector.SelectAnywhere(_flight.Centroid);
            if (target != null) Engage(target);
            else if (_engaged != null) Stop();
        }

        /// <summary>Engages radar contacts near <paramref name="center"/> and resumes the order once they are gone.</summary>
        private void DefendArea(Vector3 center)
        {
            if (SquadronTargetSelector.IsAlive(_engaged) && !_engaged.IsCloaked()) return;
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
            _weaponFireEvents.ShotEmitted -= HandleShotEmitted;
            _sFoils.Changed -= HandleSFoilsChanged;
            _shipSfxService.ReleaseShip(_entity.Value);
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

    }
}
