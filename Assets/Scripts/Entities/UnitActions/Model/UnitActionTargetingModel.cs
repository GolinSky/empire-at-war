using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;

namespace EmpireAtWar.Entities.UnitActions.Model
{
    public sealed class UnitActionTargetingModel
    {
        private readonly List<FormationPoint> _waypoints = new List<FormationPoint>();

        public event Action Changed;
        public UnitActionId? Pending { get; private set; }
        public bool IsAltPlacement { get; private set; }
        public IReadOnlyList<FormationPoint> Waypoints => _waypoints;

        public void Start(UnitActionId action, bool altPlacement = false)
        {
            Pending = action;
            IsAltPlacement = altPlacement;
            _waypoints.Clear();
            Changed?.Invoke();
        }

        public void AddWaypoint(FormationPoint point)
        {
            _waypoints.Add(point);
            Changed?.Invoke();
        }

        public void Cancel()
        {
            Pending = null;
            IsAltPlacement = false;
            _waypoints.Clear();
            Changed?.Invoke();
        }
    }
}
