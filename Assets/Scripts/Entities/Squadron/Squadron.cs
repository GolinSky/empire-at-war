using static EmpireAtWar.Utils.FormationConversion;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
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
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using EmpireAtWar.Lifecycle;
using VContainer;
using LifetimeScope = VContainer.Unity.LifetimeScope;
using VContainer.Internal;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.Squadrons
{
    /// <summary>
    /// Squadron-level order handling. The player commands the squadron; each fighter flies on its own
    /// through <see cref="SquadronPilot"/>, and weapons fire whenever a nose lines up with an enemy.
    /// A destroyed squadron is not destroyed as a GameObject: the squadron pool despawns and later respawns it.
    /// </summary>
    public sealed class Squadron : EntityView, IController, ISquadron, IInitializable, ITickable,
        ILateDisposable, IPooledEntityEvents
    {
        // Idle and guarding squadrons scan every enemy in range; spacing those scans keeps large air battles cheap.
        private const float TARGET_SEARCH_INTERVAL = 0.25f;
        // Order upkeep (target checks, arrivals, searches) runs about seven times a second, each squadron on its own
        // phase; the pilot and flight model still step every frame.
        private const float ORDER_UPDATE_INTERVAL = 0.15f;

        private ISquadronFlightComponent _flight;
        private ISquadronHealthComponent _health;
        private IRadarComponent _radar;
        private IWeaponComponent _weapon;
        private IWeaponFireEvents _weaponFireEvents;
        private IShipSfxService _shipSfxService;
        private ISelectionModelObserver _selectionModel;
        private IReadOnlyList<IAudioDialogShipComponent> _audioDialogs;
        private IAttackDataFactory _attackDataFactory;
        private ICameraService _cameraService;
        private ILayerService _layerService;
        private ShipAbilityService _abilities;
        private IShipAbilityFacade _abilityCaster;
        private CombatModifiers _modifiers;
        private IEntity _engaged;

        private UnitOrderModel _orders;
        private SquadronPilot _pilot;
        private SquadronTargetSelector _targetSelector;
        private UnitOrderSettings _orderSettings;
        private LifetimeScope _scope;
        private LifecycleTickGate _tickGate;
        private ShipSfxHandle _audioHandle;
        private SfxProfile _engineSound;
        private EntityComponentLifecycle _componentLifecycle;
        private IReadOnlyList<IRespawnable> _respawnables;
        private IReadOnlyList<IDespawnable> _despawnables;
        private SFoilsModel _sFoils;
        private IReadOnlyList<EmpireAtWar.ViewComponents.Squadrons.ISFoilsView> _sFoilsViews;

        private ShipAbilityId _pendingAbilityId;

        private float _huntRetargetTimer;
        private float _targetSearchTimer;
        private float _orderTimer;
        private float _orderElapsed;
        private float _pendingAbilityRange;
        private float _guardedRadius;

        private bool _isReleased;

        public event Action Released;
        public event Action Respawned;
        public event Action Despawned;

        private SquadronData Data { get; set; }

        public override string Id => GetType().Name;
        public Vector3 WorldPosition => _flight.Centroid;
        public float NavigationRadius => Data.NavigationRadius;
        public UnitOrderType CurrentOrder => _orders.Current;
        public bool IsDestroyed => _health.Destroyed;
        public long EntityId { get; private set; }

        /// <summary>
        /// True once nothing applied to the dead squadron is still running: abilities and timed effects would
        /// otherwise end on, or carry over to, the next life.
        /// </summary>
        public bool CanRespawn
        {
            get
            {
                if (_modifiers.HasTemporaryEffects) return false;
                foreach (ShipAbilitySlot slot in _abilityCaster.Slots)
                {
                    if (slot.State == ShipAbilityState.Active) return false;
                }

                return true;
            }
        }

        [Inject]
        private void Construct(ISquadronFlightComponent flight, ISquadronHealthComponent health, IRadarComponent radar,
            IWeaponComponent weapon, IAttackDataFactory attackDataFactory, ICameraService cameraService,
            ILayerService layerService, UnitOrderModel orders,
            SquadronPilot pilot, SquadronTargetSelector targetSelector, UnitOrderSettings orderSettings,
            LifetimeScope scope, ShipSfxHandle audioHandle, ContainerLocal<IReadOnlyList<IMonoComponent>> monoComponents,
            IWeaponFireEvents weaponFireEvents, IShipSfxService shipSfxService, ShipSfxData sfxData, SquadronType squadronType,
            SFoilsModel sFoils, ContainerLocal<IReadOnlyList<EmpireAtWar.ViewComponents.Squadrons.ISFoilsView>> sFoilsViews,
            ShipAbilityService abilities, IShipAbilityFacade abilityCaster, ISelectionModelObserver selectionModel,
            CombatModifiers modifiers, ContainerLocal<IReadOnlyList<IRespawnable>> respawnables,
            ContainerLocal<IReadOnlyList<IDespawnable>> despawnables,
            ContainerLocal<IReadOnlyList<IAudioDialogShipComponent>> audioDialogs, SquadronData data, LifecycleTickGate tickGate,
            long entityId)
        {
            EntityId = entityId;
            _flight = flight;
            _health = health;
            _radar = radar;
            _weapon = weapon;
            _weaponFireEvents = weaponFireEvents;
            _shipSfxService = shipSfxService;
            _engineSound = sfxData.GetUnit(squadronType).Engine;
            _selectionModel = selectionModel;
            _audioDialogs = audioDialogs.Value;
            _sFoils = sFoils;
            _sFoilsViews = sFoilsViews.Value;
            _orders = orders;
            _pilot = pilot;
            _targetSelector = targetSelector;
            _attackDataFactory = attackDataFactory;
            _cameraService = cameraService;
            _layerService = layerService;
            _abilities = abilities;
            _abilityCaster = abilityCaster;
            _modifiers = modifiers;
            _respawnables = respawnables.Value;
            _despawnables = despawnables.Value;
            _orderSettings = orderSettings;
            _scope = scope;
            _tickGate = tickGate;
            Data = data;
            _audioHandle = audioHandle;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents.Value);
        }

        public void Initialize()
        {
            _health.HealthModelObserver.OnDestroy += HandleDestroyed;
            _radar.Enemies.ItemAdded += HandleEnemyAdded;
            _weaponFireEvents.ShotEmitted += HandleShotEmitted;
            _sFoils.Changed += HandleSFoilsChanged;
            foreach (var view in _sFoilsViews) view.SetClosed(_sFoils.IsClosed, true);
            foreach (IAudioDialogShipComponent audioDialog in _audioDialogs)
                _selectionModel.OnSelected += audioDialog.HandleSelection;
            // A hangar issues its guard order right after creation, before the fighters have spawned.
            if (_orders.Current == UnitOrderType.Guard) EscortGuarded();
            else _pilot.Loiter(_flight.Centroid + _flight.Heading * Data.LoiterRadius);
            // Staggered so squadrons launched together do not all search in the same frame.
            _targetSearchTimer = UnityEngine.Random.Range(0f, TARGET_SEARCH_INTERVAL);
            _orderTimer = UnityEngine.Random.Range(0f, ORDER_UPDATE_INTERVAL);
            _orderElapsed = 0f;
        }

        public void LateDispose() => Release(false);

        /// <summary>Takes a squadron that never fought straight out of the battle, ready to be respawned.</summary>
        public void Stow()
        {
            Release(false);
            Despawn();
        }

        /// <summary>Takes the dead squadron out of the battle once its death presentation has ended.</summary>
        public void Despawn()
        {
            _tickGate.SetTicking(false);
            Despawned?.Invoke();
            foreach (IDespawnable part in _despawnables) part.Despawn();
            _scope.gameObject.SetActive(false);
        }

        /// <summary>Brings a despawned squadron back as a fresh one; listeners of the previous life are dropped.</summary>
        public void Respawn(Vector3 position, Quaternion rotation)
        {
            _scope.gameObject.SetActive(true);
            // Health first: restoring the fighters' views must precede the formation and trail reset.
            _health.Respawn();
            _flight.Respawn(position, rotation);
            Respawned?.Invoke();
            foreach (IRespawnable part in _respawnables) part.Respawn();
            Released = null;
            _isReleased = false;
            _componentLifecycle.Restart();
            _sFoils.SetClosed(false);
            Initialize();
            _tickGate.SetTicking(true);
        }

        public void Tick()
        {
            if (_isReleased)
            {
                return;
            }

            TickOrder(Time.deltaTime);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.SquadronPilot.Auto())
#endif
            _pilot.Tick(Time.deltaTime);
            Vector3 previousPosition = _flight.Centroid;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.SquadronFlight.Auto())
#endif
            _flight.Step(Time.deltaTime);
            if (Time.deltaTime > 0f && !_pilot.HasArrived)
            {
                float speed = Vector3.Distance(previousPosition, _flight.Centroid) / Time.deltaTime;
                if (speed > 0f)
                    _shipSfxService.TryHoldEngine(_audioHandle, _engineSound, _flight.Centroid,
                        Mathf.Clamp01(speed / _flight.Data.CruiseSpeed), 1f);
            }
        }

        private void TickOrder(float deltaTime)
        {
            _orderElapsed += deltaTime;
            _orderTimer -= deltaTime;
            if (_orderTimer <= 0f)
            {
                // Preserve the phase through hitches, skipping missed checks instead of catching up in a burst.
                _orderTimer = _orderTimer % ORDER_UPDATE_INTERVAL + ORDER_UPDATE_INTERVAL;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.SquadronOrder.Auto())
#endif
                UpdateOrder(_orderElapsed);
                _orderElapsed = 0f;
            }
        }

        private void HandleDestroyed() => Release(true);

        private void HandleSFoilsChanged()
        {
            foreach (var view in _sFoilsViews) view.SetClosed(_sFoils.IsClosed, false);
        }

        private void HandleShotEmitted(WeaponProfile profile, Transform muzzle) =>
            _shipSfxService.TryPlayWeaponShot(_audioHandle, profile, muzzle);

        public void MoveTo(Vector2 screenPosition) =>
            MoveTo(_cameraService.GetWorldPoint(screenPosition, _flight.Centroid));

        public void MoveTo(Vector3 worldPosition)
        {
            if (_orders.Matches(UnitOrderType.Move, ToPoint(worldPosition))) return;
            _orders.Replace(UnitOrderType.Move, ToPoint(worldPosition));
            PlayMoveVoice(worldPosition);
            Disengage();
            _pilot.FlyTo(worldPosition);
        }

        public void Attack(IEntity target, Vector3 formationOffset)
        {
            // Fighters swarm the target, so a formation offset has no meaning for squadrons.
            if (!SquadronTargetSelector.IsAlive(target) || target.IsCloaked() ||
                _orders.Matches(UnitOrderType.Attack, target: target)) return;
            _orders.Replace(UnitOrderType.Attack, target: target);
            foreach (IAudioDialogShipComponent audioDialog in _audioDialogs)
                audioDialog.HandleAttack(target.GetFacade<IEntityTransformFacade>().Transform.position);
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
            PlayMoveVoice(worldPosition);
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
            PlayMoveVoice(waypoints[0]);
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
            PlayMoveVoice(destination);
            Disengage();
            _pilot.FlyTo(destination);
        }

        public void Stop()
        {
            _orders.Clear();
            Disengage();
            _pilot.Loiter(_flight.Centroid);
        }

        private void PlayMoveVoice(Vector3 destination)
        {
            foreach (IAudioDialogShipComponent audioDialog in _audioDialogs) audioDialog.HandleMove(destination);
        }

        private void HandleEnemyAdded(ObservableList<IEntity> sender, ListChangedEventArgs<IEntity> args)
        {
            IEntity entity = args.item;
            if (entity.HealthModel.HasUnits && entity.TryGetFacade(out IHealthFacade healthFacade) &&
                !_weapon.HasTarget(healthFacade))
            {
                _weapon.AddTarget(new AttackData(entity.HealthModel, healthFacade, HardPointType.Any, entity),
                    AttackType.Base);
            }

            foreach (IAudioDialogShipComponent audioDialog in _audioDialogs) audioDialog.HandleEnemyDetected();
        }

        private void UpdateOrder(float elapsed)
        {
            switch (_orders.Current)
            {
                case UnitOrderType.Attack:
                    if (!SquadronTargetSelector.IsAlive(_orders.Target) || _orders.Target.IsCloaked()) Stop();
                    break;
                case UnitOrderType.Hunt:
                    UpdateHunt(elapsed);
                    break;
                case UnitOrderType.UseAbility:
                    UpdateAbilityCast();
                    break;
                case UnitOrderType.Guard:
                    if (!SquadronTargetSelector.IsAlive(_orders.Target)) Stop();
                    // Fighters orbit one side of the guarded unit, so the whole guard area is
                    // searched in what the team sees rather than in the squadron's own radar.
                    else if (ShouldSearchTargets(elapsed)) DefendArea(_targetSelector.SelectSeen(
                        _orders.Target.GetFacade<IEntityTransformFacade>().Transform.position,
                        _guardedRadius + Data.GuardRadius));
                    break;
                case UnitOrderType.AttackMove:
                    if (ShouldSearchTargets(elapsed))
                        DefendArea(_targetSelector.SelectNear(_radar.Enemies, _flight.Centroid, Data.GuardRadius));
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
                    if (ShouldSearchTargets(elapsed))
                        DefendArea(_targetSelector.SelectNear(_radar.Enemies, _pilot.LoiterCenter, Data.GuardRadius));
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

        private void UpdateHunt(float elapsed)
        {
            _huntRetargetTimer -= elapsed;
            if (SquadronTargetSelector.IsAlive(_engaged) && !_engaged.IsCloaked() && _huntRetargetTimer > 0f) return;
            _huntRetargetTimer = _orderSettings.HuntRetargetInterval;
            IEntity target = _targetSelector.SelectAnywhere(_flight.Centroid);
            if (target != null) Engage(target);
            else if (_engaged != null)
            {
                Disengage();
                _pilot.Loiter(_flight.Centroid);
            }
        }

        private bool IsEngaging => SquadronTargetSelector.IsAlive(_engaged) && !_engaged.IsCloaked();

        /// <summary>A lost target is replaced at once; with no target the search runs on an interval.</summary>
        private bool ShouldSearchTargets(float elapsed)
        {
            _targetSearchTimer -= elapsed;
            if (IsEngaging) return false;
            if (_engaged == null && _targetSearchTimer > 0f) return false;
            // Jittered: squadrons that lost one shared target search in the same frame and would otherwise stay in step.
            _targetSearchTimer = UnityEngine.Random.Range(0.5f, 1f) * TARGET_SEARCH_INTERVAL;
            return true;
        }

        /// <summary>Engages <paramref name="enemy"/> if any and resumes the order once enemies are gone.</summary>
        private void DefendArea(IEntity enemy)
        {
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
            // Fighters orbit outside the guarded hull, so the guard area grows with its size.
            _guardedRadius = SquadronPilot.GetRadius(friendly);
            _pilot.Escort(friendly.GetFacade<IEntityTransformFacade>().Transform, _guardedRadius);
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
            foreach (IAudioDialogShipComponent audioDialog in _audioDialogs)
                _selectionModel.OnSelected -= audioDialog.HandleSelection;
            _shipSfxService.ReleaseShip(_audioHandle);
            _orders.Clear();
            _engaged = null;
            _componentLifecycle.Release();
            if (destroyed)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
            }

            Released?.Invoke();
        }

    }
}
