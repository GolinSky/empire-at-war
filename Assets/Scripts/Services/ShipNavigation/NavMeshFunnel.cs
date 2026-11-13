using Unity.Collections;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal struct NavMeshFunnel
    {
        private readonly NativeArray<float2> _positions;
        private readonly NativeArray<int> _diskIndices;
        private readonly NativeArray<int> _triangles;
        private readonly NativeList<int> _reverseCorridor;
        private readonly float2 _start;
        private readonly float2 _goal;
        private NativeList<float2> _waypoints;
        private NativeList<float3> _cornerTags;

        public NavMeshFunnel(NativeArray<float2> positions, NativeArray<int> diskIndices,
            NativeArray<int> triangles, NativeList<int> corridor, float2 start, float2 goal,
            NativeList<float2> waypoints, NativeList<float3> cornerTags)
        {
            _positions = positions;
            _diskIndices = diskIndices;
            _triangles = triangles;
            _reverseCorridor = corridor;
            _start = start;
            _goal = goal;
            _waypoints = waypoints;
            _cornerTags = cornerTags;
        }

        public void Build()
        {
            float2 apex = _start;
            float2 left = apex;
            float2 right = apex;
            int apexIndex = 0;
            int leftIndex = 0;
            int rightIndex = 0;
            int leftVertex = -1;
            int rightVertex = -1;
            float leftWidth = 0f;
            float rightWidth = 0f;
            int last = _reverseCorridor.Length + 1;
            for (int i = 1; i <= last; i++)
            {
                GetPortal(i, out float2 nextLeft, out float2 nextRight, out int nextLeftVertex, out int nextRightVertex);
                float width = math.distance(nextLeft, nextRight);
                if (Turn(apex, right, nextRight) >= 0)
                {
                    if (math.all(right == apex) || Turn(apex, left, nextRight) < 0)
                    {
                        right = nextRight;
                        rightIndex = i;
                        rightVertex = nextRightVertex;
                        rightWidth = width;
                    }
                    else
                    {
                        AppendDistinct(left, leftVertex, 1, leftWidth);
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
                        leftVertex = nextLeftVertex;
                        leftWidth = width;
                    }
                    else
                    {
                        AppendDistinct(right, rightVertex, -1, rightWidth);
                        apex = right;
                        apexIndex = rightIndex;
                        left = apex;
                        leftIndex = apexIndex;
                        i = apexIndex;
                    }
                }
            }
            AppendDistinct(_goal, -1, 0, 0f);
            if (_waypoints.Length == 0)
            {
                _waypoints.Add(_goal);
                _cornerTags.Add(new float3(-1, 0, 0));
            }
        }

        private void AppendDistinct(float2 point, int vertex, int turnSign, float portalWidth)
        {
            float2 previous = _waypoints.Length == 0 ? _start : _waypoints[_waypoints.Length - 1];
            if (math.all(previous == point)) return;
            _waypoints.Add(point);
            _cornerTags.Add(new float3(vertex < 0 ? -1 : _diskIndices[vertex], turnSign, portalWidth));
        }

        private void GetPortal(int index, out float2 left, out float2 right, out int leftVertex, out int rightVertex)
        {
            if (index > _reverseCorridor.Length)
            {
                left = right = _goal;
                leftVertex = rightVertex = -1;
                return;
            }
            // The entered triangle is clockwise, so its first edge vertex is on the right.
            int edge = _reverseCorridor[_reverseCorridor.Length - index];
            int next = edge % 3 == 2 ? edge - 2 : edge + 1;
            rightVertex = _triangles[edge];
            leftVertex = _triangles[next];
            right = _positions[rightVertex];
            left = _positions[leftVertex];
        }

        private static double Turn(float2 apex, float2 a, float2 b)
        {
            double2 lhs = (double2)a - (double2)apex;
            double2 rhs = (double2)b - (double2)apex;
            return lhs.x * rhs.y - lhs.y * rhs.x;
        }
    }
}
