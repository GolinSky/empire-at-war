using EmpireAtWar.Components.Ship.Movement;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct ShipTangentRouteJob : IJob
    {
        // Burst turns a throw into an abort in Player builds; failures are returned instead.
        public const int FAILURE_NONE = 0;
        public const int FAILURE_NO_TANGENT = 1;
        public const int FAILURE_ARC_REPAIR = 2;
        public const int FAILURE_LINE_REPAIR = 3;

        [ReadOnly] public NativeArray<float3> Disks;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> DiskBuckets;
        public NavMeshBuckets Layout;
        public NativeList<ShipRouteCircle> Circles;
        public NativeList<ShipRoutePiece> Pieces;
        public NativeReference<int> Failure;
        public float2 Start;
        public float Height;
        public bool Escape;

        public void Execute()
        {
            Failure.Value = FAILURE_NONE;
            for (int i = 0; i < Circles.Length; i++)
            {
                ShipRouteCircle circle = Circles[i];
                circle.Previous = i - 1;
                circle.Next = i + 1 < Circles.Length ? i + 1 : -1;
                Circles[i] = circle;
            }
            CheckStart();
            if (!RepairTangents()) return;
            if (CheckStart() && !RepairTangents()) return;
            Emit();
        }

        private bool RepairTangents()
        {
            // Each repair changes only the adjoining tangents. A removed corner is never
            // revisited; a pierced disk is inserted at the first intersecting leg.
            int index = 0;
            int repairs = 0;
            int repairLimit = (Disks.Length + Circles.Length) * 4 + 16;
            while (Circles[index].Next >= 0)
            {
                ShipRouteCircle a = Circles[index];
                ShipRouteCircle b = Circles[a.Next];
                if (!ShipRouteGeometry.Tangent(a, b, out float2 from, out float2 to))
                {
                    // The heading-constrained start circle may touch a disk that a repair
                    // later makes its neighbour. Turning in place at the start is always valid.
                    if (index == 0 && a.Radius > 0f)
                    {
                        a.Center = Start;
                        a.Radius = 0f;
                        Circles[0] = a;
                        continue;
                    }
                    Failure.Value = FAILURE_NO_TANGENT;
                    return false;
                }

                if (index > 0 && a.Radius > 0f)
                {
                    ShipRouteGeometry.Tangent(Circles[a.Previous], a, out _, out float2 entry);
                    float sweep = ShipRouteGeometry.Sweep(entry - a.Center, from - a.Center, a.Sign);
                    if (sweep < 0f && a.SweepHint > math.PI) sweep += 2f * math.PI;
                    if (sweep <= ShipRouteGeometry.EPSILON)
                    {
                        ShipRouteCircle previous = Circles[a.Previous];
                        previous.Next = a.Next;
                        b.Previous = a.Previous;
                        Circles[a.Previous] = previous;
                        Circles[a.Next] = b;
                        index = a.Previous;
                        continue;
                    }
                    ShipRoutePiece arc = ShipRouteGeometry.Arc(a, entry, sweep, Height);
                    int arcBlocker = ShipRouteGeometry.FindBlocker(arc, Disks, DiskBuckets, Layout, a.DiskIndex);
                    if (arcBlocker >= 0)
                    {
                        if (++repairs > repairLimit)
                        {
                            Failure.Value = FAILURE_ARC_REPAIR;
                            return false;
                        }
                        float3 disk = Disks[arcBlocker];
                        a.SweepHint = 0f;
                        Circles[index] = a;
                        int inserted = InsertAfter(index, new ShipRouteCircle(disk.xy, disk.z, a.Sign, arcBlocker));
                        InsertAfter(inserted, a);
                        index = math.max(0, a.Previous);
                        continue;
                    }
                }

                ShipRoutePiece line = ShipRouteGeometry.Line(from, to, Height);
                int blocker = Escape && index == 0 ? -1 : ShipRouteGeometry.FindBlocker(
                    line, Disks, DiskBuckets, Layout, a.DiskIndex, b.DiskIndex);
                if (blocker >= 0)
                {
                    if (++repairs > repairLimit)
                    {
                        Failure.Value = FAILURE_LINE_REPAIR;
                        return false;
                    }
                    float3 disk = Disks[blocker];
                    float side = ShipRouteGeometry.Cross(to - from, disk.xy - from);
                    int sign = side > 0f ? -1 : side < 0f ? 1 : (b.Sign != 0 ? b.Sign : a.Sign);
                    if (sign == 0) sign = 1;
                    InsertAfter(index, new ShipRouteCircle(disk.xy, disk.z, sign, blocker));
                    index = math.max(0, a.Previous);
                    continue;
                }
                index = a.Next;
            }
            return true;
        }

        private bool CheckStart()
        {
            // Only the heading-constrained start can leave the funnel corridor. If its
            // bounded check fails, replace that start circle with the safe funnel apex.
            ShipRouteCircle first = Circles[0];
            if (first.Radius > 0f)
            {
                bool hasTangent = ShipRouteGeometry.Tangent(first, Circles[first.Next], out float2 exit, out float2 entry);
                float sweep = ShipRouteGeometry.Sweep(Start - first.Center, exit - first.Center, first.Sign);
                if (sweep < -ShipRouteGeometry.EPSILON) sweep += 2f * math.PI;
                sweep = math.max(0f, sweep);
                ShipRoutePiece arc = ShipRouteGeometry.Arc(first, Start, sweep, Height);
                ShipRoutePiece leg = ShipRouteGeometry.Line(exit, entry, Height);
                if (!hasTangent || !ShipRouteGeometry.InsideBounds(arc, Layout) || !ShipRouteGeometry.InsideBounds(leg, Layout) ||
                    ShipRouteGeometry.FindBlocker(arc, Disks, DiskBuckets, Layout) >= 0 ||
                    ShipRouteGeometry.FindBlocker(leg, Disks, DiskBuckets, Layout, -1, Circles[first.Next].DiskIndex) >= 0)
                {
                    first.Center = Start;
                    first.Radius = 0f;
                    Circles[0] = first;
                    return true;
                }
            }
            return false;
        }

        private int InsertAfter(int index, ShipRouteCircle circle)
        {
            ShipRouteCircle previous = Circles[index];
            ShipRouteCircle next = Circles[previous.Next];
            circle.Previous = index;
            circle.Next = previous.Next;
            previous.Next = Circles.Length;
            next.Previous = Circles.Length;
            Circles[index] = previous;
            Circles[circle.Next] = next;
            Circles.Add(circle);
            return Circles.Length - 1;
        }

        private void Emit()
        {
            float2 entry = Start;
            for (int i = 0; Circles[i].Next >= 0; i = Circles[i].Next)
            {
                ShipRouteCircle circle = Circles[i];
                ShipRouteGeometry.Tangent(circle, Circles[circle.Next], out float2 exit, out float2 nextEntry);
                if (circle.Radius > 0f)
                {
                    float sweep = ShipRouteGeometry.Sweep(entry - circle.Center, exit - circle.Center, circle.Sign);
                    if (sweep < -ShipRouteGeometry.EPSILON) sweep += 2f * math.PI;
                    if (sweep > ShipRouteGeometry.EPSILON)
                        Pieces.Add(ShipRouteGeometry.Arc(circle, entry, sweep, Height));
                }
                if (math.distancesq(exit, nextEntry) > ShipRouteGeometry.EPSILON * ShipRouteGeometry.EPSILON)
                    Pieces.Add(ShipRouteGeometry.Line(exit, nextEntry, Height));
                entry = nextEntry;
            }
        }
    }
}
