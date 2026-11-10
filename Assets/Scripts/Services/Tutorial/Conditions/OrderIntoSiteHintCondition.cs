using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Met when the player orders units (move, attack-move or waypoints, from the map or the minimap) into a
    /// not-owned site of the step's target kind. The units need not arrive or stay selected.
    /// </summary>
    public sealed class OrderIntoSiteHintCondition : IHintCondition
    {
        private readonly IUnitOrderService _orders;
        private readonly ICaptureSiteDirectory _sites;
        private readonly ILocalPlayer _player;
        private readonly HintTargets _targets;
        private readonly HintTargetId _target;

        public bool IsMet { get; private set; }

        public OrderIntoSiteHintCondition(IUnitOrderService orders, ICaptureSiteDirectory sites, ILocalPlayer player,
            HintTargets targets, HintTargetId target)
        {
            _orders = orders;
            _sites = sites;
            _player = player;
            _targets = targets;
            _target = target;
            _orders.OrderIssued += HandleOrderIssued;
        }

        private void HandleOrderIssued(UnitOrder order)
        {
            if (!_player.IsLocal(order.Issuer) || !TryGetDestination(order, out Vector3 destination)) return;
            foreach (ICaptureSite site in _sites.Sites)
                if (_targets.IsSiteOf(_target, site) && !_player.IsLocal(site.Owner) && site.Contains(destination))
                    IsMet = true;
        }

        private static bool TryGetDestination(UnitOrder order, out Vector3 destination)
        {
            switch (order.Action)
            {
                case UnitActionId.Move:
                case UnitActionId.AttackMove:
                    destination = order.Point;
                    return true;
                case UnitActionId.WaypointMove:
                    IReadOnlyList<Vector3> waypoints = order.Waypoints;
                    destination = waypoints != null && waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : order.Point;
                    return true;
                default:
                    destination = default;
                    return false;
            }
        }

        public void Dispose() => _orders.OrderIssued -= HandleOrderIssued;
    }
}
