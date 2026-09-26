using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.UnitOrders
{
    public sealed class PlayerOrderInputHandler : IInitializable, ILateDisposable,
        IPlayerOrderInputHandler
    {
        private readonly IInputService _input;
        private readonly ISelectionService _selection;
        private readonly ISelectionQuery _query;
        private readonly ICameraService _camera;
        private readonly ILayerService _layers;
        private readonly IShipAbilityTargeting _abilities;
        private readonly UnitActionTargetingModel _targeting;
        private readonly IUnitOrderService _orders;
        private readonly SuperWeaponTargetingModel _superWeapons;

        public PlayerOrderInputHandler(IInputService input, ISelectionService selection,
            ISelectionQuery query, ICameraService camera, ILayerService layers,
            IShipAbilityTargeting abilities, UnitActionTargetingModel targeting,
            IUnitOrderService orders, SuperWeaponTargetingModel superWeapons)
        {
            _input = input;
            _selection = selection;
            _query = query;
            _camera = camera;
            _layers = layers;
            _abilities = abilities;
            _targeting = targeting;
            _orders = orders;
            _superWeapons = superWeapons;
        }

        public void Initialize()
        {
            _input.OnInput += HandleInput;
            _input.OnWaypointModifierReleased += HandleModifierReleased;
        }

        public void LateDispose()
        {
            _input.OnInput -= HandleInput;
            _input.OnWaypointModifierReleased -= HandleModifierReleased;
        }

        public void FinishWaypoints()
        {
            if (_targeting.Pending != UnitActionId.WaypointMove) return;
            if (_targeting.Waypoints.Count > 0)
            {
                List<Vector3> points = new List<Vector3>(_targeting.Waypoints.Count);
                foreach (FormationPoint waypoint in _targeting.Waypoints)
                    points.Add(new Vector3(waypoint.X, 0f, waypoint.Z));
                _orders.IssueWaypointMove(Snapshot(), points);
            }
            _targeting.Cancel();
        }

        public bool TryIssueMove(Vector3 worldPoint)
        {
            List<IEntity> receivers = Snapshot();
            foreach (IEntity receiver in receivers)
            {
                if (!receiver.TryGetFacade(out IMoveFacade move)) continue;
                worldPoint.y = move.WorldPosition.y;
                _orders.IssueMove(receivers, worldPoint);
                return true;
            }
            return false;
        }

        private void HandleModifierReleased()
        {
            if (_targeting.IsAltPlacement) FinishWaypoints();
        }

        private void HandleInput(InputType type, TouchPhase phase, Vector2 screen)
        {
            if (type != InputType.ShipInput) return;
            bool hasUnit = _query.TryFindAt(screen, out SelectionEntry hit);
            IEntity target = hasUnit ? hit.Entity : null;
            if (_superWeapons.Pending != null)
            {
                if (target != null) _superWeapons.Submit(target);
                else _superWeapons.Cancel();
                return;
            }

            if (_abilities.IsWaitingForTarget)
            {
                if (target != null && target.PlayerType == PlayerType.Opponent)
                    _abilities.SubmitTarget(target);
                else _abilities.CancelTargeting();
                return;
            }

            List<IEntity> receivers = Snapshot();
            if (receivers.Count == 0) return;
            if (_targeting.Pending == null && _input.IsWaypointModifierPressed)
            {
                foreach (IEntity receiver in receivers)
                    if (receiver.TryGetFacade(out IWaypointMoveFacade _))
                    {
                        _targeting.Start(UnitActionId.WaypointMove, altPlacement: true);
                        break;
                    }
            }
            UnitActionId? pending = _targeting.Pending;
            if (pending == UnitActionId.Attack)
            {
                if (IsEnemy(target))
                {
                    _orders.IssueAttack(receivers, target);
                    _targeting.Cancel();
                }
                return;
            }
            if (pending == UnitActionId.Guard)
            {
                if (target != null && target.PlayerType == PlayerType.Player &&
                    target.TryGetFacade(out IEntitySelectionFacade _))
                {
                    _orders.IssueGuard(receivers, target);
                    _targeting.Cancel();
                }
                return;
            }
            if (pending != null)
            {
                if (hasUnit || IsObstacle(screen)) return;
                Vector3 point = _camera.GetWorldPoint(screen, ReferencePosition(receivers));
                switch (pending.Value)
                {
                    case UnitActionId.AttackMove:
                        _orders.IssueAttackMove(receivers, point);
                        _targeting.Cancel();
                        break;
                    case UnitActionId.WaypointMove:
                        _targeting.AddWaypoint(new FormationPoint(point.x, point.z));
                        break;
                }
                return;
            }

            if (IsEnemy(target)) _orders.IssueAttack(receivers, target);
            else if (!hasUnit && !IsObstacle(screen))
                _orders.IssueMove(receivers,
                    _camera.GetWorldPoint(screen, ReferencePosition(receivers)));
        }

        private List<IEntity> Snapshot()
        {
            List<IEntity> receivers = new List<IEntity>();
            foreach (IEntity entity in _selection.PlayerSelectionContext.Entities)
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

        private static bool IsEnemy(IEntity entity) =>
            entity != null && entity.PlayerType == PlayerType.Opponent &&
            !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits;

        private bool IsObstacle(Vector2 screen)
        {
            RaycastHit hit = _camera.ScreenPointToRay(screen);
            return hit.collider != null &&
                   _layers.IsInLayer(hit.collider.gameObject, LayerKey.Obstacle);
        }
    }
}
