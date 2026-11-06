using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavMeshRouteJob : IJob
    {
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<int> Triangles;
        [ReadOnly] public NativeArray<int> Halfedges;
        [ReadOnly] public NativeArray<int> Regions;
        [ReadOnly] public NativeArray<float> ExtraCost;
        public NavigationGridCells Cells;
        public bool HasExtraCost;
        public int StartTriangle;
        public int GoalTriangle;
        public float2 Start;
        public float2 Goal;
        public int Generation;
        public NativeArray<SearchNode> Nodes;
        public NativeArray<int> CorridorGenerations;
        public NativeList<HeapEntry> Open;
        public NativeList<int> Corridor;
        public NativeList<float2> Waypoints;

        public void Execute()
        {
            Waypoints.Clear();
            Corridor.Clear();
            Open.Clear();
            if (StartTriangle < 0 || GoalTriangle < 0 || Regions[StartTriangle] == 0 ||
                Regions[StartTriangle] != Regions[GoalTriangle])
                return;

            // The entry portal fixes the position in a triangle. Different entries must
            // remain distinct states because they have different costs to the next exit.
            int startNode = Halfedges.Length;
            int goalNode = startNode + 1;
            Relax(startNode, -1, 0f, Start);
            while (Open.Length > 0)
            {
                int current = Pop();
                SearchNode record = Nodes[current];
                if (record.Generation == -Generation)
                    continue;
                if (current == goalNode)
                {
                    BuildWaypoints(record.Parent, startNode);
                    return;
                }

                record.Generation = -Generation;
                Nodes[current] = record;
                int triangle = current == startNode ? StartTriangle : current / 3;
                float2 entry = current == startNode ? Start : Midpoint(current);
                float weight = 1f;
                if (HasExtraCost)
                {
                    int first = triangle * 3;
                    float2 center = (Positions[Triangles[first]] + Positions[Triangles[first + 1]] +
                                     Positions[Triangles[first + 2]]) / 3f;
                    weight += ExtraCost[Cells.GetCell(center)];
                }

                if (triangle == GoalTriangle)
                    Relax(goalNode, current, record.Cost + weight * math.distance(entry, Goal), Goal);
                for (int edge = triangle * 3; edge < triangle * 3 + 3; edge++)
                {
                    int neighbor = Halfedges[edge];
                    if (edge == current || neighbor < 0 || Regions[neighbor / 3] == 0)
                        continue;
                    float2 exit = Midpoint(edge);
                    Relax(neighbor, current, record.Cost + weight * math.distance(entry, exit), exit);
                }
            }
        }

        private void BuildWaypoints(int edge, int startNode)
        {
            // An expensive triangle can be cheaper to leave and re-enter through a
            // different portal. Such a corridor overlaps itself and has no simple funnel.
            bool revisitsTriangle = false;
            CorridorGenerations[StartTriangle] = Generation;
            while (edge != startNode)
            {
                Corridor.Add(edge);
                int triangle = edge / 3;
                if (CorridorGenerations[triangle] == Generation)
                    revisitsTriangle = true;
                CorridorGenerations[triangle] = Generation;
                edge = Nodes[edge].Parent;
            }
            var funnel = new NavMeshFunnel(Positions, Triangles, Corridor, Start, Goal, Cells, ExtraCost, HasExtraCost,
                Waypoints);
            if (revisitsTriangle)
                funnel.UsePortalMidpoints();
            else
                funnel.Build();
        }

        private float2 Midpoint(int edge)
        {
            int next = edge % 3 == 2 ? edge - 2 : edge + 1;
            return (Positions[Triangles[edge]] + Positions[Triangles[next]]) * 0.5f;
        }

        private void Relax(int node, int parent, float cost, float2 position)
        {
            SearchNode previous = Nodes[node];
            if (previous.Generation == -Generation || previous.Generation == Generation && cost >= previous.Cost)
                return;
            Nodes[node] = new SearchNode { Generation = Generation, Parent = parent, Cost = cost };
            HeapEntry entry = new HeapEntry { Node = node, Priority = cost + math.distance(position, Goal) };
            int index = Open.Length;
            Open.Add(entry);
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (!entry.Precedes(Open[parentIndex]))
                    break;
                Open[index] = Open[parentIndex];
                index = parentIndex;
            }
            Open[index] = entry;
        }

        private int Pop()
        {
            int node = Open[0].Node;
            int last = Open.Length - 1;
            HeapEntry entry = Open[last];
            Open.RemoveAt(last);
            if (last == 0)
                return node;

            int index = 0;
            for (int child = 1; child < last; child = index * 2 + 1)
            {
                if (child + 1 < last && Open[child + 1].Precedes(Open[child]))
                    child++;
                if (!Open[child].Precedes(entry))
                    break;
                Open[index] = Open[child];
                index = child;
            }
            Open[index] = entry;
            return node;
        }

        internal struct SearchNode
        {
            public int Generation;
            public int Parent;
            public float Cost;
        }

        internal struct HeapEntry
        {
            public float Priority;
            public int Node;

            public bool Precedes(HeapEntry other)
            {
                return Priority < other.Priority || Priority == other.Priority && Node < other.Node;
            }
        }
    }
}
