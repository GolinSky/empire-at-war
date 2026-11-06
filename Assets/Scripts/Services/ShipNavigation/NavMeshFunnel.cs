using Unity.Collections;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal struct NavMeshFunnel
    {
        private const float COST_TOLERANCE = 0.01f;
        private readonly NativeArray<float2> _positions;
        private readonly NativeArray<int> _triangles;
        private readonly NativeList<int> _reverseCorridor;
        private readonly NavigationTravelCost _travelCost;
        private readonly bool _hasExtraCost;
        private readonly float2 _start;
        private readonly float2 _goal;
        private NativeList<float2> _waypoints;

        public NavMeshFunnel(NativeArray<float2> positions, NativeArray<int> triangles, NativeList<int> corridor,
            float2 start, float2 goal, NavigationGridCells cells, NativeArray<float> extraCost, bool hasExtraCost,
            NativeList<float2> waypoints)
        {
            _positions = positions;
            _triangles = triangles;
            _reverseCorridor = corridor;
            _start = start;
            _goal = goal;
            _travelCost = new NavigationTravelCost(cells, extraCost);
            _hasExtraCost = hasExtraCost;
            _waypoints = waypoints;
        }

        public void Build()
        {
            float2 apex = _start;
            float2 left = apex;
            float2 right = apex;
            int apexIndex = 0;
            int leftIndex = 0;
            int rightIndex = 0;
            int last = _reverseCorridor.Length + 1;
            for (int i = 1; i <= last; i++)
            {
                GetPortal(i, out float2 nextLeft, out float2 nextRight);
                if (Turn(apex, right, nextRight) >= 0)
                {
                    if (math.all(right == apex) || Turn(apex, left, nextRight) < 0)
                    {
                        right = nextRight;
                        rightIndex = i;
                    }
                    else
                    {
                        AppendLeg(apexIndex, apex, leftIndex, left);
                        apex = left;
                        apexIndex = leftIndex;
                        right = apex;
                        rightIndex = apexIndex;
                        i = apexIndex;
                        continue;
                    }
                }

                if (Turn(apex, left, nextLeft) <= 0)
                {
                    if (math.all(left == apex) || Turn(apex, right, nextLeft) > 0)
                    {
                        left = nextLeft;
                        leftIndex = i;
                    }
                    else
                    {
                        AppendLeg(apexIndex, apex, rightIndex, right);
                        apex = right;
                        apexIndex = rightIndex;
                        left = apex;
                        leftIndex = apexIndex;
                        i = apexIndex;
                    }
                }
            }
            AppendLeg(apexIndex, apex, last, _goal);
            if (_waypoints.Length == 0)
                _waypoints.Add(_goal);
            if (_hasExtraCost && _reverseCorridor.Length != 0)
                PreserveCorridorCost();
        }

        // A corner on a portal can itself lie inside a hazard even when its midpoint is
        // clear. Guard that change of endpoint as well as the shortcuts between corners.
        private void PreserveCorridorCost()
        {
            float2 previous = _start;
            float cost = 0f;
            float length = 0f;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                float2 point = _waypoints[i];
                cost += _travelCost.Segment(previous, point);
                length += math.distance(previous, point);
                previous = point;
            }
            if (cost <= length + COST_TOLERANCE)
                return;

            float baseline = 0f;
            previous = _start;
            for (int i = 1; i <= _reverseCorridor.Length; i++)
            {
                float2 midpoint = Midpoint(i);
                baseline += _travelCost.Segment(previous, midpoint);
                previous = midpoint;
            }
            if (cost <= baseline + _travelCost.Segment(previous, _goal) + COST_TOLERANCE)
                return;

            UsePortalMidpoints();
        }

        public void UsePortalMidpoints()
        {
            _waypoints.Clear();
            for (int i = 1; i <= _reverseCorridor.Length; i++)
                AppendDistinct(Midpoint(i));
            AppendDistinct(_goal);
            if (_waypoints.Length == 0)
                _waypoints.Add(_goal);
        }

        private void AppendLeg(int fromIndex, float2 from, int toIndex, float2 to)
        {
            // Each midpoint leg lies in a single walkable triangle. Restore those legs
            // if the geometric shortcut would cut across a more expensive hazard.
            if (_hasExtraCost && toIndex > fromIndex + 1)
            {
                float shortcut = _travelCost.Segment(from, to);
                if (shortcut > math.distance(from, to) + COST_TOLERANCE)
                {
                    float original = 0f;
                    float2 previous = from;
                    for (int i = fromIndex + 1; i < toIndex; i++)
                    {
                        float2 midpoint = Midpoint(i);
                        original += _travelCost.Segment(previous, midpoint);
                        previous = midpoint;
                    }
                    if (shortcut > original + _travelCost.Segment(previous, to) + COST_TOLERANCE)
                    {
                        for (int i = fromIndex + 1; i < toIndex; i++)
                            AppendDistinct(Midpoint(i));
                    }
                }
            }
            AppendDistinct(to);
        }

        private void AppendDistinct(float2 point)
        {
            float2 previous = _waypoints.Length == 0 ? _start : _waypoints[_waypoints.Length - 1];
            if (math.any(previous != point))
                _waypoints.Add(point);
        }

        private float2 Midpoint(int index)
        {
            GetPortal(index, out float2 left, out float2 right);
            return (left + right) * 0.5f;
        }

        private void GetPortal(int index, out float2 left, out float2 right)
        {
            if (index > _reverseCorridor.Length)
            {
                left = right = _goal;
                return;
            }
            // This is the edge in the triangle being entered. Its clockwise winding
            // puts its first vertex on the right when crossing into that triangle.
            int edge = _reverseCorridor[_reverseCorridor.Length - index];
            int next = edge % 3 == 2 ? edge - 2 : edge + 1;
            right = _positions[_triangles[edge]];
            left = _positions[_triangles[next]];
        }

        private static double Turn(float2 apex, float2 a, float2 b)
        {
            double2 lhs = (double2)a - (double2)apex;
            double2 rhs = (double2)b - (double2)apex;
            return lhs.x * rhs.y - lhs.y * rhs.x;
        }
    }
}
