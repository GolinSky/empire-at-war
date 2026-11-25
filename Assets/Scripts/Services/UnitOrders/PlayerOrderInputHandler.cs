using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Health;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.UnitOrders
{
    [LifecycleOrder(Initialize = LifecycleOrders.ORDER_INPUT_INITIALIZE)]
    public sealed class PlayerOrderInputHandler : IInitializable, ILateDisposable,
        IPlayerOrderInputHandler
    {
        private const float SATELLITE_TAP_PADDING_PIXELS = 4f;
        private static readonly Vector2[] SatelliteTapOffsets =
            { Vector2.zero, Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        private readonly IReadOnlyList<ICaptureSiteDirectory> _siteDirectories;
        private readonly IPointerGestures _gestures;
        private readonly IUnitOrderInput _orderInput;
        private readonly ILocalPlayer _localPlayer;
        private readonly ISelectionService _selectionService;
        private readonly ISelectionQuery _query;
        private readonly ICameraService _cameraService;
        private readonly ILayerService _layerService;
        private readonly IShipAbilityTargeting _abilities;
        private readonly IPlayerUnitOrders _playerOrders;
        private readonly IHardPointHoverObserver _hardPointHover;
        private readonly IBattleAreaService _battleAreas;
        private readonly List<SelectionEntry> _attackCandidates = new List<SelectionEntry>();
        private SelectionEntry _pressedMarker;
        private IEntity _pressedHardPointOwner;
        private int _pressedHardPointId;
        private bool _hasPressedHardPoint;
        // The last command tap issued an attack-move: a repeat tap re-issues it without pacing.
        private bool _lastTapAttackMoved;

        private readonly UnitActionTargetingModel _targeting;
        private readonly SuperWeaponTargetingModel _superWeapons;

        public PlayerOrderInputHandler(IPointerGestures gestures, IUnitOrderInput orderInput,
            ISelectionService selectionService,
            ISelectionQuery query, ICameraService cameraService, ILayerService layerService,
            IShipAbilityTargeting abilities, IPlayerUnitOrders playerOrders,
            IHardPointHoverObserver hardPointHover, ILocalPlayer localPlayer,
            UnitActionTargetingModel targeting,
            SuperWeaponTargetingModel superWeapons, IBattleAreaService battleAreas,
            IReadOnlyList<ICaptureSiteDirectory> siteDirectories)
        {
            _siteDirectories = siteDirectories;
            _localPlayer = localPlayer;
            _gestures = gestures;
            _orderInput = orderInput;
            _selectionService = selectionService;
            _query = query;
            _cameraService = cameraService;
            _layerService = layerService;
            _abilities = abilities;
            _targeting = targeting;
            _playerOrders = playerOrders;
            _superWeapons = superWeapons;
            _hardPointHover = hardPointHover;
            _battleAreas = battleAreas;
        }

        public void Initialize()
        {
            _gestures.WorldCommandPressed += HandleCommandPressed;
            _gestures.WorldCommanded += HandleCommand;
            _orderInput.QueueWaypointReleased += HandleModifierReleased;
        }

        public void LateDispose()
        {
            _gestures.WorldCommandPressed -= HandleCommandPressed;
            _gestures.WorldCommanded -= HandleCommand;
            _orderInput.QueueWaypointReleased -= HandleModifierReleased;
            _pressedMarker = default;
            _pressedHardPointOwner = null;
            _hasPressedHardPoint = false;
        }

        public void FinishWaypoints()
        {
            if (_targeting.Pending != UnitActionId.WaypointMove) return;
            if (_targeting.Waypoints.Count > 0)
            {
                List<Vector3> points = new List<Vector3>(_targeting.Waypoints.Count);
                foreach (FormationPoint waypoint in _targeting.Waypoints)
                    points.Add(ToVector(waypoint));
                _playerOrders.IssueWaypointMove(Snapshot(), points);
            }
            _targeting.Cancel();
        }

        public bool TryIssueMove(Vector3 worldPoint) => TryIssueMove(worldPoint, MovePace.Formation);

        private bool TryIssueMove(Vector3 worldPoint, MovePace pace, bool attackMove = false)
        {
            if (_abilities.IsWaitingForTarget || _superWeapons.Pending != null ||
                _targeting.Pending == UnitActionId.Attack || _targeting.Pending == UnitActionId.Guard)
                return false;
            List<IEntity> receivers = Snapshot();
            foreach (IEntity receiver in receivers)
            {
                if (!receiver.TryGetFacade(out IMoveFacade move)) continue;
                if (_battleAreas.TryGetOrderSnap(worldPoint, out Vector3 center))
                    worldPoint = center;
                worldPoint.y = move.WorldPosition.y;
                if (attackMove || _targeting.Pending == UnitActionId.AttackMove)
                {
                    _playerOrders.IssueAttackMove(receivers, worldPoint, pace);
                    if (_targeting.Pending == UnitActionId.AttackMove) _targeting.Cancel();
                    _lastTapAttackMoved = true;
                }
                else if (_targeting.Pending == UnitActionId.WaypointMove)
                    _targeting.AddWaypoint(ToPoint(worldPoint));
                else
                    _playerOrders.IssueMove(receivers, worldPoint, pace);
                return true;
            }
            return false;
        }

        private void HandleModifierReleased()
        {
            if (_targeting.IsAltPlacement) FinishWaypoints();
        }

        private void HandleCommandPressed(Vector2 screen)
        {
            _pressedMarker = default;
            _hasPressedHardPoint = TryGetEnemyHardPoint(out _pressedHardPointOwner, out _pressedHardPointId);
            if (!_hasPressedHardPoint)
                _query.TryFindMarkerAt(screen, out _pressedMarker);
        }

        private void HandleCommand(Vector2 screen)
        {
            bool isRepeatTap = _gestures.CommandClickCount >= 2;
            bool repeatsAttackMove = _lastTapAttackMoved && isRepeatTap && _targeting.Pending == null &&
                                     !_abilities.IsWaitingForTarget && _superWeapons.Pending == null;
            _lastTapAttackMoved = false;
            if (IsAttackMoveArmed() || repeatsAttackMove)
            {
                _pressedMarker = default;
                _pressedHardPointOwner = null;
                _hasPressedHardPoint = false;
                IssueAttackMoveAt(screen, repeatsAttackMove ? MovePace.Loose : MovePace.Formation);
                return;
            }

            SelectionEntry hit = _pressedMarker;
            _pressedMarker = default;
            bool capturedMarker = hit.IsBattleMarker;
            if (capturedMarker && !_query.IsMarkerAvailable(hit))
                return;

            // A hardpoint marker wins over the hull or whatever else is under it.
            IEntity hardPointOwner = _pressedHardPointOwner;
            int hardPointId = _pressedHardPointId;
            bool hasHardPoint = _hasPressedHardPoint;
            _pressedHardPointOwner = null;
            _hasPressedHardPoint = false;
            if (!hasHardPoint && !capturedMarker)
                hasHardPoint = TryGetEnemyHardPoint(out hardPointOwner, out hardPointId);
            bool hasUnit = capturedMarker || hasHardPoint || _query.TryFindAt(screen, out hit);
            IEntity target = hasHardPoint ? hardPointOwner : hasUnit ? hit.Entity : null;
            if (_superWeapons.Pending != null)
            {
                if (target != null) _superWeapons.Submit(target);
                else _superWeapons.Cancel();
                return;
            }

            if (_abilities.IsWaitingForTarget)
            {
                if (!capturedMarker && !hasHardPoint && !IsEnemy(target) &&
                    TryFindEnemyAt(screen, out SelectionEntry enemy))
                    target = enemy.Entity;
                if (target != null && _localPlayer.IsHostile(target.Owner))
                    _abilities.SubmitTarget(target);
                else _abilities.CancelTargeting();
                return;
            }

            if (hasHardPoint)
            {
                if (hardPointOwner.HealthModel.IsDestroyed) return;
                IReadOnlyList<IReadOnlyHardPointModel> hardPoints = hardPointOwner.HealthModel.HardPointModels;
                if (hardPointId >= hardPoints.Count || !hardPoints[hardPointId].IsInstalled ||
                    hardPoints[hardPointId].IsDestroyed) return;
            }

            List<IEntity> receivers = Snapshot();
            if (receivers.Count == 0) return;
            if (_targeting.Pending == null && _orderInput.IsQueueWaypointHeld)
            {
                foreach (IEntity receiver in receivers)
                    if (receiver.TryGetFacade(out IWaypointMoveFacade _))
                    {
                        _targeting.Start(UnitActionId.WaypointMove, altPlacement: true);
                        break;
                    }
            }
            UnitActionId? pending = _targeting.Pending;
            if (!capturedMarker && !hasHardPoint && !IsEnemy(target) &&
                (pending == null || pending == UnitActionId.Attack) &&
                TryFindEnemyAt(screen, out SelectionEntry enemyBelow))
            {
                hit = enemyBelow;
                target = enemyBelow.Entity;
                hasUnit = true;
            }
            if (pending == UnitActionId.Attack)
            {
                // A wrong target leaves the mode instead of keeping it armed without feedback.
                if (IsEnemy(target))
                    IssueAttack(receivers, target, hasHardPoint, hardPointId, MovePace.Formation);
                _targeting.Cancel();
                return;
            }
            if (pending == UnitActionId.Guard)
            {
                // Allied units can be escorted too.
                if (target != null && _localPlayer.IsFriendly(target.Owner) &&
                    target.TryGetFacade(out IEntitySelectionFacade _))
                    _playerOrders.IssueGuard(receivers, target);
                _targeting.Cancel();
                return;
            }
            // Friendly hulls can cover a site without blocking commands to its center.
            if (!hasHardPoint && !IsEnemy(target) && TryGetOrderSnap(screen, out Vector3 snap))
            {
                MovePace snapPace = pending == null && _gestures.CommandClickCount >= 2
                    ? MovePace.Loose
                    : MovePace.Formation;
                TryIssueMove(snap, snapPace);
                return;
            }
            bool isFriendlyShip = hasUnit && !hasHardPoint && _localPlayer.IsFriendly(target.Owner) &&
                hit.Command.SelectionType == SelectionType.Ship;
            if (pending != null)
            {
                if (hasUnit && !isFriendlyShip || IsObstacle(screen)) return;
                Vector3 point = _cameraService.GetWorldPoint(screen, ReferencePosition(receivers));
                if (pending == UnitActionId.WaypointMove) _targeting.AddWaypoint(ToPoint(point));
                return;
            }

            MovePace pace = isRepeatTap ? MovePace.Loose : MovePace.Formation;
            if (IsEnemy(target)) IssueAttack(receivers, target, hasHardPoint, hardPointId, pace);
            else if ((!hasUnit || isFriendlyShip) && !IsObstacle(screen))
            {
                _playerOrders.IssueMove(receivers,
                    _cameraService.GetWorldPoint(screen, ReferencePosition(receivers)), pace);
            }
        }

        // Attack-move orders the ground under the cursor: units, hardpoints and markers never block it.
        public bool CanIssueAttackMove(Vector2 screen) => TryGetOrderSnap(screen, out _) || !IsObstacle(screen);

        private bool IsAttackMoveArmed() =>
            _targeting.Pending == UnitActionId.AttackMove && !_abilities.IsWaitingForTarget &&
            _superWeapons.Pending == null;

        private void IssueAttackMoveAt(Vector2 screen, MovePace pace)
        {
            if (TryGetOrderSnap(screen, out Vector3 snap))
            {
                TryIssueMove(snap, pace, attackMove: true);
                return;
            }

            if (IsObstacle(screen)) return;
            List<IEntity> receivers = Snapshot();
            if (receivers.Count == 0) return;
            _playerOrders.IssueAttackMove(receivers,
                _cameraService.GetWorldPoint(screen, ReferencePosition(receivers)), pace);
            _targeting.Cancel();
            _lastTapAttackMoved = true;
        }

        private bool TryGetOrderSnap(Vector2 screen, out Vector3 point)
        {
            foreach (ICaptureSiteDirectory directory in _siteDirectories)
            {
                foreach (Vector2 offset in SatelliteTapOffsets)
                {
                    Ray ray = _cameraService.SceneCamera.ScreenPointToRay(screen + offset * SATELLITE_TAP_PADDING_PIXELS);
                    ICaptureSite site = CaptureSitePointerQuery.Find(directory.Sites, ray, CaptureSiteKind.Satellite);
                    if (site == null) continue;
                    point = site.Center;
                    return true;
                }
            }
            foreach (OwnedCircle area in _battleAreas.Get(BattleAreaLayer.OrderSnap))
                if (area.Contains(_cameraService.GetWorldPoint(screen, area.Transform.position), 0f))
                {
                    point = area.Transform.position;
                    return true;
                }
            point = default;
            return false;
        }

        private void IssueAttack(List<IEntity> receivers, IEntity target, bool hasHardPoint, int hardPointId,
            MovePace pace)
        {
            if (hasHardPoint) _playerOrders.IssueHardPointAttack(receivers, target, hardPointId, pace);
            else _playerOrders.IssueAttack(receivers, target, pace);
        }

        private List<IEntity> Snapshot()
        {
            List<IEntity> receivers = new List<IEntity>();
            foreach (IEntity entity in _selectionService.PlayerSelectionContext.Entities)
                if (!entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits)
                    receivers.Add(entity);
            return receivers;
        }

        // Project clicks onto the plane of the ships being ordered, as ships fly at
        // different heights and the camera is angled.
        private static Vector3 ReferencePosition(IReadOnlyList<IEntity> receivers)
        {
            foreach (IEntity receiver in receivers)
                if (receiver.TryGetFacade(out IMoveFacade move)) return move.WorldPosition;
            return receivers[0].GetFacade<IEntityTransformFacade>().Transform.position;
        }

        private bool IsEnemy(IEntity entity) =>
            entity != null && _localPlayer.IsHostile(entity.Owner) &&
            !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits;

        // Friendly hardpoint markers only show status; a command over them goes to the hull or space beneath.
        private bool TryGetEnemyHardPoint(out IEntity owner, out int hardPointId) =>
            _hardPointHover.TryGetAttackTarget(out owner, out hardPointId) && IsEnemy(owner);

        // Friendly hulls must not block attacks or ability targets on an enemy underneath them.
        private bool TryFindEnemyAt(Vector2 screen, out SelectionEntry enemy)
        {
            _attackCandidates.Clear();
            _query.CollectAt(screen, _attackCandidates);
            foreach (SelectionEntry candidate in _attackCandidates)
            {
                if (!IsEnemy(candidate.Entity)) continue;
                enemy = candidate;
                return true;
            }

            enemy = default;
            return false;
        }

        private bool IsObstacle(Vector2 screen)
        {
            RaycastHit hit = _cameraService.ScreenPointToRay(screen);
            return hit.collider != null &&
                   _layerService.IsInLayer(hit.collider.gameObject, LayerKey.Obstacle);
        }
    }
}
