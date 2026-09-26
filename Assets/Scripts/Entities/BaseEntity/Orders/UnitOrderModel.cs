using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Entities.BaseEntity.Orders
{
    public sealed class UnitOrderModel
    {
        public const int NO_HARD_POINT = -1;
        private const float POSITION_EPSILON_SQUARED = 0.01f;
        private readonly List<FormationPoint> _waypoints = new List<FormationPoint>();

        public UnitOrderType Current { get; private set; }
        public FormationPoint Destination { get; private set; }
        public IEntity Target { get; private set; }
        public int TargetHardPointId { get; private set; } = NO_HARD_POINT;
        public FormationPoint Offset { get; private set; }
        public IReadOnlyList<FormationPoint> Waypoints => _waypoints;
        public int WaypointIndex { get; private set; }

        public void Replace(UnitOrderType type, FormationPoint destination = default,
            IEntity target = null, FormationPoint offset = default,
            IReadOnlyList<FormationPoint> waypoints = null, int targetHardPointId = NO_HARD_POINT)
        {
            Current = type;
            Destination = destination;
            Target = target;
            TargetHardPointId = targetHardPointId;
            Offset = offset;
            _waypoints.Clear();
            if (waypoints != null) _waypoints.AddRange(waypoints);
            WaypointIndex = 0;
        }

        public void Clear() => Replace(UnitOrderType.None);

        public bool Matches(UnitOrderType type, FormationPoint destination = default,
            IEntity target = null, FormationPoint offset = default, int targetHardPointId = NO_HARD_POINT)
        {
            return Current == type && TargetHardPointId == targetHardPointId &&
                   (Target == null ? target == null : target != null && Target.Id == target.Id) &&
                   Near(Destination, destination) && Near(Offset, offset);
        }

        public bool MatchesWaypoints(IReadOnlyList<FormationPoint> waypoints)
        {
            if (Current != UnitOrderType.WaypointMove || _waypoints.Count != waypoints.Count) return false;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (!Near(_waypoints[i], waypoints[i])) return false;
            }

            return true;
        }

        public bool AdvanceWaypoint(out FormationPoint waypoint)
        {
            WaypointIndex++;
            if (Current == UnitOrderType.WaypointMove && WaypointIndex < _waypoints.Count)
            {
                waypoint = _waypoints[WaypointIndex];
                Destination = waypoint;
                return true;
            }

            waypoint = default;
            return false;
        }

        private static bool Near(FormationPoint first, FormationPoint second)
        {
            float x = first.X - second.X;
            float z = first.Z - second.Z;
            return x * x + z * z <= POSITION_EPSILON_SQUARED;
        }
    }
}
