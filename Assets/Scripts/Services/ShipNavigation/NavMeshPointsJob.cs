using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavMeshPointsJob : IJob
    {
        public NavMeshBuckets Layout;
        public float CellSize;
        [ReadOnly] public NativeArray<float3> Disks;
        [ReadOnly] public NativeArray<float2> HazardPoints;
        public NativeParallelMultiHashMap<int, int> DiskBuckets;
        public NativeList<float2> Points;
        public NativeList<int> DiskIndices;

        public void Execute()
        {
            float2 min = Layout.Min;
            float2 max = Layout.Max;
            float magnitude = math.cmax(math.max(math.abs(min), math.abs(max)));
            float spacing = math.max(0.00001f, magnitude * 0.0000004f);
            var unique = new NativeParallelMultiHashMap<int2, int>(128, Allocator.Temp);
            // Preserve the rectangular hull even when every corner lies inside a disk.
            AddUnique(min, unique, spacing);
            AddUnique(max, unique, spacing);
            AddUnique(new float2(min.x, max.y), unique, spacing);
            AddUnique(new float2(max.x, min.y), unique, spacing);
            for (int i = 1; i < NavMeshBuckets.SIDE; i++)
            {
                float2 edge = math.lerp(min, max, (float)i / NavMeshBuckets.SIDE);
                AddFree(new float2(edge.x, min.y), unique, spacing);
                AddFree(new float2(edge.x, max.y), unique, spacing);
                AddFree(new float2(min.x, edge.y), unique, spacing);
                AddFree(new float2(max.x, edge.y), unique, spacing);
            }

            for (int i = 0; i < Disks.Length; i++)
            {
                float3 disk = Disks[i];
                if (disk.z <= 0f) continue;
                // Chords stay within one raster cell at every clearance, so large ships keep grid-level
                // gap precision. Cells are at least 1/160 of the map, which bounds the count.
                int count = math.max(8, (int)math.ceil(2f * math.PI * disk.z / CellSize));
                // Keep tangent chords outside even after conversion of world coordinates to float.
                float outer = (disk.z + 4f * spacing) / math.cos(math.PI / count);
                float2 previous = disk.xy + new float2(outer, 0f);
                for (int j = 1; j <= count; j++)
                {
                    math.sincos((j % count) * (2f * math.PI / count), out float sin, out float cos);
                    float2 current = disk.xy + outer * new float2(cos, sin);
                    AddFree(current, unique, spacing, i);
                    // A disk crossing the map edge needs a vertex where its tangent chord enters the map.
                    AddCrossings(previous, current, 0, unique, spacing);
                    AddCrossings(previous, current, 1, unique, spacing);
                    previous = current;
                }
            }

            for (int i = 0; i < HazardPoints.Length; i++) AddFree(HazardPoints[i], unique, spacing);
            unique.Dispose();
        }

        private void AddCrossings(float2 a, float2 b, int axis, NativeParallelMultiHashMap<int2, int> unique, float spacing)
        {
            if (a[axis] == b[axis]) return;
            for (int side = 0; side < 2; side++)
            {
                float boundary = side == 0 ? Layout.Min[axis] : Layout.Max[axis];
                float t = (boundary - a[axis]) / (b[axis] - a[axis]);
                if (t <= 0f || t >= 1f) continue;
                float2 point = math.lerp(a, b, t);
                point[axis] = boundary;
                AddFree(point, unique, spacing);
            }
        }

        private void AddFree(float2 point, NativeParallelMultiHashMap<int2, int> unique, float spacing, int diskIndex = -1)
        {
            if (math.any(point < Layout.Min) || math.any(point > Layout.Max)) return;
            int2 cell = Layout.Cell(point);
            foreach (int index in DiskBuckets.GetValuesForKey(NavMeshBuckets.Key(cell.x, cell.y)))
            {
                float3 disk = Disks[index];
                if (math.distancesq(point, disk.xy) < disk.z * disk.z) return;
            }
            AddUnique(point, unique, spacing, diskIndex);
        }

        private void AddUnique(float2 point, NativeParallelMultiHashMap<int2, int> unique, float spacing, int diskIndex = -1)
        {
            int2 cell = (int2)math.floor((point - Layout.Min) / spacing);
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
            {
                foreach (int index in unique.GetValuesForKey(cell + new int2(x, y)))
                    if (math.distancesq(Points[index], point) <= spacing * spacing) return;
            }
            unique.Add(cell, Points.Length);
            Points.Add(point);
            DiskIndices.Add(diskIndex);
        }
    }
}
