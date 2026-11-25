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
using EmpireAtWar.Components.Squadrons.Icon;
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
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Utils;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEngine;
using UnityEngine.Rendering;
using EmpireAtWar.Lifecycle;
using VContainer;
using VContainer.Internal;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Models.Players;

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
        // Dev telemetry: one periodic order/position row per squadron.
        private const float TELEMETRY_SAMPLE_INTERVAL = 5f;
        // An escort holds the ship's flank this share of the ship's radius behind its centre, never ahead of it.
        private const float ESCORT_TRAIL_SHARE = 0.5f;
        private float _nextTelemetrySample;

        private ISquadronFlightComponent _flight;
        private SquadronIconComponent _icon;
        private bool _isReplica;
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
        // Host-only and not saved, like the ships' pace.
        private PaceCluster _pace;
        private int _paceMember;
        private float _stragglerTimer;
        private bool _isEscorting;

        private UnitOrderModel _orders;
        private SquadronPilot _pilot;
        private SquadronReplicaAnchor _replicaAnchor;
        private SquadronTargetSelector _targetSelector;
        private IFocusFireClaim _focusFire;
        private SquadronOrderScheduler _orderScheduler;
        private UnitOrderSettings _orderSettings;
        private IEntityPoolActivation _poolActivation;
        private LifecycleTickGate _tickGate;
        private FighterPoseBatch _poseBatch;
        private ShipSfxHandle _audioHandle;
        [SerializeField] private bool playDestructionSound;
        private ShipUnitAudioProfile _unitAudio;
        private SfxProfile _moveSound;
        private EntityComponentLifecycle _componentLifecycle;
        private IReadOnlyList<IRespawnable> _respawnables;
        private IReadOnlyList<IDespawnable> _despawnables;
        private SFoilsModel _sFoils;
        private IReadOnlyList<ISFoilsComponent> _sFoilsViews;

        private ShipAbilityId _pendingAbilityId;
        private PlayerId _owner;
        private SquadronType _squadronType;

        private float _huntRetargetTimer;
        private float _targetSearchTimer;
        private float _pendingAbilityRange;
        private float _guardedRadius;

        private bool _isReleased;
        // An attack-move flies to its point without breaking off for fights on the way; set on first arrival.
        private bool _hasReachedAttackMovePoint;

        public event Action Released;
        public event Action Respawned;
        public event Action Despawned;

        private SquadronData Data { get; set; }

        public override string Id => GetType().Name;
        public Vector3 WorldPosition => _flight.Centroid;
        public float NavigationRadius => Data.NavigationRadius;
        // Online clients show the host's order (see ApplyReplicaOrder).
        public UnitOrderType CurrentOrder => _replicaOrder ?? _orders.Current;
        internal IEntity AttackTarget => CurrentOrder == UnitOrderType.Attack ? _orders.Target : null;
        public int OrderRevision => _orders.Revision;
        private UnitOrderType? _replicaOrder;
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
            IEntityPoolActivation poolActivation, ShipSfxHandle audioHandle, ContainerLocal<IReadOnlyList<IMonoComponent>> monoComponents,
            IWeaponFireEvents weaponFireEvents, IShipSfxService shipSfxService, ShipSfxData sfxData, SquadronType squadronType,
            SFoilsModel sFoils, ContainerLocal<IReadOnlyList<ISFoilsComponent>> sFoilsViews,
            ShipAbilityService abilities, IShipAbilityFacade abilityCaster, ISelectionModelObserver selectionModel,
            CombatModifiers modifiers, ContainerLocal<IReadOnlyList<IRespawnable>> respawnables,
            ContainerLocal<IReadOnlyList<IDespawnable>> despawnables,
            ContainerLocal<IReadOnlyList<IAudioDialogShipComponent>> audioDialogs, SquadronData data, LifecycleTickGate tickGate,
            long entityId, FighterPoseBatch poseBatch, SquadronOrderScheduler orderScheduler, PlayerId owner,
            SquadronIconComponent icon, IMatchSession session, SquadronReplicaAnchor replicaAnchor,
            IFocusFireClaim focusFire)
        {
            _focusFire = focusFire;
            _replicaAnchor = replicaAnchor;
            EntityId = entityId;
            _owner = owner;
            _squadronType = squadronType;
            _flight = flight;
            _icon = icon;
            _isReplica = !session.IsAuthority;
            _health = health;
            _radar = radar;
            _weapon = weapon;
            _weaponFireEvents = weaponFireEvents;
            _shipSfxService = shipSfxService;
            _unitAudio = sfxData.GetUnit(squadronType);
            _moveSound = sfxData.Acceleration;
            _selectionModel = selectionModel;
            _audioDialogs = audioDialogs.Value;
            _sFoils = sFoils;
            _sFoilsViews = sFoilsViews.Value;
            _orders = orders;
            _pilot = pilot;
            _targetSelector = targetSelector;
            _orderScheduler = orderScheduler;
            _attackDataFactory = attackDataFactory;
            _cameraService = cameraService;
            _layerService = layerService;
            _abilities = abilities;
            _abilityCaster = abilityCaster;
            _modifiers = modifiers;
            _respawnables = respawnables.Value;
            _despawnables = despawnables.Value;
            _orderSettings = orderSettings;
            _poolActivation = poolActivation;
            _tickGate = tickGate;
            _poseBatch = poseBatch;
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
            _replicaAnchor.Reset();
            // Staggered so squadrons launched together do not all search in the same frame.
            _targetSearchTimer = UnityEngine.Random.Range(0f, TARGET_SEARCH_INTERVAL);
            _orderScheduler.Register(UpdateOrder);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _nextTelemetrySample = Time.time + TELEMETRY_SAMPLE_INTERVAL;
            RecordTelemetry("start", "Squadron entered the battle.");
#endif
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
            _poseBatch.CompletePlacement();
            _tickGate.SetTicking(false);
            Despawned?.Invoke();
            foreach (IDespawnable part in _despawnables) part.Despawn();
            _poolActivation.SetActive(false);
        }

        /// <summary>Brings a despawned squadron back as a fresh one; listeners of the previous life are dropped.</summary>
        public void Respawn(Vector3 position, Quaternion rotation)
        {
            _poseBatch.CompletePlacement();
            _poolActivation.SetActive(true);
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

        public void Tick() => Tick(Time.deltaTime);

        internal void Tick(float deltaTime)
        {
            if (_isReleased)
            {
                return;
            }

            // Order searches are budgeted, but the pilot reads target views every frame.
            // Retire dead targets before their cached hardpoint transforms can be read.
            if ((_orders.Current == UnitOrderType.Attack || _orders.Current == UnitOrderType.Guard) &&
                !SquadronTargetSelector.IsAlive(_orders.Target))
                Stop();
            else if (_engaged != null && !SquadronTargetSelector.IsAlive(_engaged))
            {
                Disengage();
                ResumeCourse();
            }

            // On replicas the root still holds the host pose ClientReplicationReceiver applied in the last LateTick.
            Vector3 previousPosition = _flight.Centroid;
            if (_isReplica) _replicaAnchor.Steer(previousPosition, deltaTime);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.SquadronPilot.Auto())
#endif
            _pilot.Tick(deltaTime);
            UpdatePace(deltaTime);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.SquadronFlight.Auto())
#endif
            _flight.Step(deltaTime);
            // Local flight has overwritten the root; the marker follows the replicated motion, not the fighters.
            Vector3 markerCentroid = _isReplica ? previousPosition : _flight.Centroid;
            _icon.AdvanceAnchor(markerCentroid, deltaTime);
            if (deltaTime > 0f && !_pilot.HasArrived)
            {
                float speed = Vector3.Distance(previousPosition, _flight.Centroid) / deltaTime;
                if (speed > 0f)
                    _shipSfxService.TryHoldEngine(_audioHandle, _unitAudio.Engine, _flight.Centroid,
                        Mathf.Clamp01(speed / _flight.Data.CruiseSpeed), 1f);
            }
        }

        private void HandleDestroyed() => Release(true);

        private void HandleSFoilsChanged()
        {
            foreach (var view in _sFoilsViews) view.SetClosed(_sFoils.IsClosed, false);
        }

        private void HandleShotEmitted(WeaponProfile profile, Transform muzzle, float shotInterval) =>
            _shipSfxService.TryPlayWeaponShot(_audioHandle, profile, muzzle, shotInterval);

        public void MoveTo(Vector2 screenPosition) =>
            MoveTo(_cameraService.GetWorldPoint(screenPosition, _flight.Centroid));

        public void MoveTo(Vector3 worldPosition, PaceCluster pace = null)
        {
            if (_orders.Matches(UnitOrderType.Move, ToPoint(worldPosition)))
            {
                JoinPace(pace);
                return;
            }
            _orders.Replace(UnitOrderType.Move, ToPoint(worldPosition));
            PlayMoveAudio(worldPosition);
            Disengage();
            _pilot.FlyTo(worldPosition);
            JoinPace(pace);
        }

        public void Attack(IEntity target, Vector3 formationOffset, PaceCluster pace = null)
        {
            // Fighters swarm the target, so a formation offset has no meaning for squadrons.
            if (!SquadronTargetSelector.IsAlive(target) || target.IsCloaked()) return;
            if (!_orders.Matches(UnitOrderType.Attack, target: target))
            {
                _orders.Replace(UnitOrderType.Attack, target: target);
                foreach (IAudioDialogShipComponent audioDialog in _audioDialogs)
                    audioDialog.HandleAttack(target.GetFacade<IEntityTransformFacade>().Transform.position);
                Engage(target);
            }

            JoinPace(_pilot.IsInAttackZone ? null : pace);
        }

        // Approach speed for the pace cluster; a squadron that cannot fly never holds the cluster back.
        internal float PaceSpeed
        {
            get
            {
                float speed = _flight.Data.CombatSpeed * _modifiers.SpeedMultiplier;
                return _modifiers.IsIonDisabled || speed <= 0f ? float.PositiveInfinity : speed;
            }
        }

        // A repeated order joins its new cluster; the squadron leaves the old one first.
        private void JoinPace(PaceCluster pace)
        {
            LeavePace();
            if (pace == null || !IsPacedOrder(_orders.Current)) return;
            SamplePaceGeometry(out System.Numerics.Vector2 position, out float remaining,
                out System.Numerics.Vector2 heading);
            _pace = pace;
            _paceMember = pace.Join(PaceSpeed, remaining, position, NavigationRadius, heading, isEscort: true);
        }

        private void LeavePace()
        {
            _stragglerTimer = 0f;
            if (_pace == null) return;
            _pace.Leave(_paceMember);
            _pace = null;
            _flight.SetSpeedCap(float.PositiveInfinity);
            if (!_isEscorting) return;
            _isEscorting = false;
            ResumeEscortedCourse();
        }

        // Escorting ended (ships arrived, engaged or left, or a new order): the squadron flies its own order again.
        // A pilot already engaging or given a new course keeps it.
        private void ResumeEscortedCourse()
        {
            if (!_pilot.IsFollowing) return;
            switch (_orders.Current)
            {
                case UnitOrderType.Attack when SquadronTargetSelector.IsAlive(_orders.Target):
                    _pilot.Engage(_orders.Target);
                    break;
                case UnitOrderType.Move:
                case UnitOrderType.AttackMove:
                    _pilot.FlyTo(ToVector(_orders.Destination));
                    break;
                default:
                    _pilot.Loiter(_flight.Centroid);
                    break;
            }
        }

        // A squadron sharing a cluster with ships flies beside the nearest one, on its flank and slightly behind,
        // instead of slowing down ahead of the group where it meets the enemy alone.
        private void EscortShip(System.Numerics.Vector2 anchorPosition, System.Numerics.Vector2 anchorHeading,
            float anchorRadius)
        {
            _isEscorting = true;
            _flight.SetSpeedCap(float.PositiveInfinity);
            Vector3 center = new Vector3(anchorPosition.X, 0f, anchorPosition.Y);
            Vector3 forward = new Vector3(anchorHeading.X, 0f, anchorHeading.Y);
            Vector3 fromShip = _flight.Centroid - center;
            fromShip.y = 0f;
            float standOff = anchorRadius + _flight.Data.LoiterRadius;
            Vector3 point;
            if (forward.sqrMagnitude > 1e-6f)
            {
                Vector3 side = new Vector3(forward.z, 0f, -forward.x);
                if (Vector3.Dot(fromShip, side) < 0f) side = -side;
                point = center + side * standOff - forward * (anchorRadius * ESCORT_TRAIL_SHARE);
            }
            else
            {
                point = center + (fromShip.sqrMagnitude > 1e-6f ? fromShip.normalized : Vector3.right) * standOff;
            }

            float pace = _pace.Pace;
            _pilot.Follow(point, forward, float.IsPositiveInfinity(pace) ? _flight.Data.CruiseSpeed : pace);
        }

        // Ships set the pace and squadrons escort them; squadrons alone approach at their slowest squadron's speed.
        // Fighters break away freely once in the attack zone or engaging.
        private void UpdatePace(float deltaTime)
        {
            if (_pace == null) return;
            if (!IsPacedOrder(_orders.Current) || _pilot.HasArrived ||
                (_orders.Current == UnitOrderType.Attack && _pilot.IsInAttackZone) ||
                (_orders.Current == UnitOrderType.AttackMove && _engaged != null))
            {
                LeavePace();
                return;
            }

            float ownSpeed = PaceSpeed;
            if (_pace.IsBelowStragglerSpeed(ownSpeed))
            {
                LeavePace();
                return;
            }

            SamplePaceGeometry(out System.Numerics.Vector2 position, out float remaining,
                out System.Numerics.Vector2 heading);
            _pace.Report(_paceMember, ownSpeed, remaining, position, heading);
            if (_pace.TryGetEscortAnchor(position, out System.Numerics.Vector2 anchorPosition,
                    out System.Numerics.Vector2 anchorHeading, out float anchorRadius))
            {
                EscortShip(anchorPosition, anchorHeading, anchorRadius);
                return;
            }

            if (_isEscorting)
            {
                LeavePace();
                return;
            }

            if (!_pace.TryGetSpeedCap(remaining, deltaTime, ref _stragglerTimer, out float cap))
            {
                LeavePace();
                return;
            }

            _flight.SetSpeedCap(cap);
        }

        private static bool IsPacedOrder(UnitOrderType order) =>
            order == UnitOrderType.Move || order == UnitOrderType.Attack || order == UnitOrderType.AttackMove;

        private void SamplePaceGeometry(out System.Numerics.Vector2 position, out float remaining,
            out System.Numerics.Vector2 heading)
        {
            Vector3 current = _flight.Centroid;
            position = new System.Numerics.Vector2(current.x, current.z);
            Vector3 goal;
            if (_orders.Current == UnitOrderType.Attack)
            {
                IEntity target = _orders.Target;
                if (!SquadronTargetSelector.IsAlive(target))
                {
                    remaining = float.NaN;
                    heading = default;
                    return;
                }

                goal = target.GetFacade<IEntityTransformFacade>().Transform.position;
            }
            else
            {
                goal = ToVector(_orders.Destination);
            }

            System.Numerics.Vector2 delta = new System.Numerics.Vector2(goal.x - current.x, goal.z - current.z);
            float distSq = delta.LengthSquared();
            if (distSq > 1e-6f)
            {
                remaining = MathF.Sqrt(distSq);
                heading = delta / remaining;
            }
            else
            {
                remaining = 0f;
                heading = default;
            }
        }

        public void CastAbility(ShipAbilityId id, IEntity target, float range)
        {
            LeavePace();
            _pendingAbilityId = id;
            _pendingAbilityRange = range;
            _orders.Replace(UnitOrderType.UseAbility, target: target);
            Engage(target);
        }

        public void AttackMoveTo(Vector3 worldPosition) => AttackMoveTo(worldPosition, null);

        public void AttackMoveTo(Vector3 worldPosition, PaceCluster pace)
        {
            if (_orders.Matches(UnitOrderType.AttackMove, ToPoint(worldPosition)))
            {
                JoinPace(_engaged == null ? pace : null);
                return;
            }
            _orders.Replace(UnitOrderType.AttackMove, ToPoint(worldPosition));
            _hasReachedAttackMovePoint = false;
            PlayMoveAudio(worldPosition);
            Disengage();
            _pilot.FlyTo(worldPosition);
            JoinPace(pace);
        }

        public void Guard(IEntity friendly, Vector3 offset)
        {
            if (!SquadronTargetSelector.IsAlive(friendly) ||
                _orders.Matches(UnitOrderType.Guard, target: friendly)) return;
            LeavePace();
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
            LeavePace();
            _orders.Replace(UnitOrderType.WaypointMove, points[0], waypoints: points);
            PlayMoveAudio(waypoints[0]);
            Disengage();
            _pilot.FlyTo(waypoints[0]);
        }

        public void Hunt()
        {
            if (_orders.Current == UnitOrderType.Hunt) return;
            LeavePace();
            _orders.Replace(UnitOrderType.Hunt);
            _huntRetargetTimer = 0f;
        }

        public void Retreat(Vector3 destination)
        {
            if (_orders.Matches(UnitOrderType.Retreat, ToPoint(destination))) return;
            LeavePace();
            _orders.Replace(UnitOrderType.Retreat, ToPoint(destination));
            PlayMoveAudio(destination);
            Disengage();
            _pilot.FlyTo(destination);
        }

        public void Stop()
        {
            LeavePace();
            _orders.Clear();
            Disengage();
            _pilot.Loiter(_flight.Centroid);
        }

        /// <summary>
        /// Online client: the host's order. Clients only replay attacks, for the fighters' swarm; once the host
        /// gives another order the replayed engagement is dropped. New moves get their sound and voice line.
        /// </summary>
        public void ApplyReplicaOrder(UnitOrderType order, bool isNewOrder)
        {
            _replicaOrder = order;
            if (!IsEngagingOrder(order) && IsEngagingOrder(_orders.Current)) Stop();
            if (isNewOrder && order.IsMoveOrder()) PlayMoveAudio(_flight.Centroid);
        }

        private static bool IsEngagingOrder(UnitOrderType order) =>
            order.IsAttackOrder() || order == UnitOrderType.UseAbility;

        private void PlayMoveAudio(Vector3 destination)
        {
            _shipSfxService.TryPlayOneShot(_audioHandle, _moveSound, _flight.Centroid);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Time.time >= _nextTelemetrySample)
            {
                _nextTelemetrySample = Time.time + TELEMETRY_SAMPLE_INTERVAL;
                RecordTelemetry("sample", "Periodic squadron order state.");
            }
#endif
            // Online clients do not pick their own fights: the host's squadron is followed (SquadronReplicaAnchor)
            // and only a replayed attack drives the fighters.
            if (_isReplica)
            {
                if (_orders.Current == UnitOrderType.Attack &&
                    (!SquadronTargetSelector.IsAlive(_orders.Target) || _orders.Target.IsCloaked())) Stop();
                return;
            }

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
                    if (!SquadronTargetSelector.IsAlive(_orders.Target))
                    {
                        Stop();
                        break;
                    }

                    // Fighters orbit one side of the guarded unit, so the whole guard area is
                    // searched in what the team sees rather than in the squadron's own radar.
                    // The area reaches past the guarded unit's guns, so escorts join the fight it is in.
                    Vector3 guardedPosition = _orders.Target.GetFacade<IEntityTransformFacade>().Transform.position;
                    float guardRadius = Mathf.Max(_guardedRadius + Data.GuardRadius,
                        _orderSettings.SquadronGuardEngageRadius);
                    if (!IsGuardedUnitTarget(_engaged) && BreakOffBeyondLeash(guardedPosition, guardRadius)) break;
                    if (ShouldSearchTargets(elapsed))
                        DefendArea(_targetSelector.SelectGuardTarget(_orders.Target, guardedPosition, guardRadius));
                    break;
                case UnitOrderType.AttackMove:
                    if (!_hasReachedAttackMovePoint)
                    {
                        if (!_pilot.HasArrived) break;
                        _hasReachedAttackMovePoint = true;
                        // Search on arrival, before the empty-handed order clears below.
                        _targetSearchTimer = 0f;
                    }
                    if (ShouldSearchTargets(elapsed))
                        DefendArea(_targetSelector.SelectNear(_radar, _flight.Centroid, Data.GuardRadius));
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
                    if (BreakOffBeyondLeash(_pilot.LoiterCenter, Data.GuardRadius)) break;
                    if (ShouldSearchTargets(elapsed))
                        DefendArea(_targetSelector.SelectNear(_radar, _pilot.LoiterCenter, Data.GuardRadius));
                    break;
            }
        }

        /// <summary>
        /// Guard and idle only: a target chased farther than the engage radius times the leash factor from
        /// <paramref name="anchor"/> is dropped and the squadron returns to its post or escort.
        /// Reacquiring needs the target back inside the engage radius, so the gap between both radii stops flip-flops.
        /// </summary>
        private bool BreakOffBeyondLeash(Vector3 anchor, float engageRadius)
        {
            if (!IsEngaging) return false;
            float leash = engageRadius * _orderSettings.SquadronLeashFactor;
            Vector3 target = _engaged.GetFacade<IEntityTransformFacade>().Transform.position;
            if ((target - anchor).sqrMagnitude <= leash * leash) return false;
            Disengage();
            ResumeCourse();
            return true;
        }

        /// <summary>The guarded unit's guns already bound the distance to its own target, so no leash applies.</summary>
        private bool IsGuardedUnitTarget(IEntity target) =>
            target != null && _orders.Target.TryGetFacade(out IUnitCombatObserverFacade combat) &&
            ReferenceEquals(combat.Target, target);

        private void UpdateAbilityCast()
        {
            IEntity target = _orders.Target;
            if (!SquadronTargetSelector.IsAlive(target) || target.IsCloaked())
            {
                Stop();
                return;
            }

            if (!ShipAbilityRange.IsInRange(_abilityCaster, target, _pendingAbilityRange)) return;
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

        public bool IsEngaging => SquadronTargetSelector.IsAlive(_engaged) && !_engaged.IsCloaked();

        public float IdleLeashRadius => Data.GuardRadius * _orderSettings.SquadronLeashFactor;

        private bool IsBomberOnStrikecraft =>
            _targetSelector.PrefersShips && _engaged.HealthModel.ShipClass.IsStrikecraft();

        /// <summary>
        /// A lost target is replaced at once; with no target the search runs on an interval.
        /// Bombers fighting strikecraft keep searching on the interval for a ship.
        /// </summary>
        private bool ShouldSearchTargets(float elapsed)
        {
            _targetSearchTimer -= elapsed;
            if (IsEngaging && !IsBomberOnStrikecraft) return false;
            if ((_engaged == null || IsEngaging) && _targetSearchTimer > 0f) return false;
            // Jittered: squadrons that lost one shared target search in the same frame and would otherwise stay in step.
            _targetSearchTimer = UnityEngine.Random.Range(0.5f, 1f) * TARGET_SEARCH_INTERVAL;
            return true;
        }

        /// <summary>Engages <paramref name="enemy"/> if any and resumes the order once enemies are gone.</summary>
        private void DefendArea(IEntity enemy)
        {
            if (enemy != null)
            {
                // A bomber already fighting strikecraft breaks off only for a ship.
                if (IsEngaging && enemy.HealthModel.ShipClass.IsStrikecraft()) return;
                Engage(enemy);
                return;
            }

            if (_engaged == null || IsEngaging) return;
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
                case UnitOrderType.None:
                    // Idle squadrons return to their post instead of loitering where the fight ended.
                    _pilot.FlyTo(_pilot.LoiterCenter);
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
            // Attack-move fights and ability casts claim no focus fire slot; every other main target tries to.
            if (_orders.Current == UnitOrderType.AttackMove || _orders.Current == UnitOrderType.UseAbility)
                _focusFire.Release();
            else _focusFire.TryClaim(target);
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
            _focusFire.Release();
            _weapon.ResetTarget();
        }

        private void RecordTelemetry(string kind, string reason)
        {
            if (!Application.isPlaying) return;

            IEntity target = _orders.Target;
            // Sampling and release can run before the order retires a destroyed target.
            float targetDistance = !SquadronTargetSelector.IsAlive(target) ? -1f :
                PlanarGeometry.Distance(_flight.Centroid, target.GetFacade<IEntityTransformFacade>().Transform.position);
            SquadronTelemetry.Squadron(kind, _owner, EntityId, _squadronType.ToString(), _orders.Current.ToString(),
                target == null ? 0L : target.Id, _engaged == null ? 0L : _engaged.Id, _flight.Centroid,
                targetDistance, _health.HealthModelObserver.Hull, _health.HealthModelObserver.Shields, reason);
        }

        private void Release(bool destroyed)
        {
            _health.HealthModelObserver.OnDestroy -= HandleDestroyed;
            if (_isReleased)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (destroyed) RecordTelemetry("destroyed", "Squadron destroyed.");
#endif
            _isReleased = true;
            LeavePace();
            _orderScheduler.Unregister(UpdateOrder);
            _radar.Enemies.ItemAdded -= HandleEnemyAdded;
            _weaponFireEvents.ShotEmitted -= HandleShotEmitted;
            _sFoils.Changed -= HandleSFoilsChanged;
            foreach (IAudioDialogShipComponent audioDialog in _audioDialogs)
                _selectionModel.OnSelected -= audioDialog.HandleSelection;
            _shipSfxService.ReleaseShip(_audioHandle,
                destroyed && playDestructionSound ? _unitAudio.Explosion : null, _flight.Centroid);
            _orders.Clear();
            _engaged = null;
            _focusFire.Release();
            _componentLifecycle.Release();
            if (destroyed)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
            }

            Released?.Invoke();
        }

    }
}
