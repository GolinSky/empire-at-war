using EmpireAtWar.Services.ShipNavigation;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class NavigationGridCellsTests
    {
        [Test]
        public void FindNearestWalkableCell_MatchesFullGridScan()
        {
            Random random = new Random(1234u);
            for (int iteration = 0; iteration < 300; iteration++)
            {
                int width = random.NextInt(1, 40);
                int height = random.NextInt(1, 40);
                NavigationGridCells cells = new NavigationGridCells(new float2(-50f, 20f), 2.5f, width, height);
                float blockedChance = random.NextFloat(0.3f, 1f);
                NativeArray<byte> blocked = new NativeArray<byte>(width * height, Allocator.Temp);
                for (int i = 0; i < blocked.Length; i++) blocked[i] = random.NextFloat() < blockedChance ? (byte)1 : (byte)0;
                // Positions also fall outside the grid, where the queried cell is clamped to the edge.
                float2 position = new float2(-50f, 20f) + random.NextFloat2(new float2(-30f), new float2(130f));

                Assert.That(cells.FindNearestWalkableCell(blocked, position), Is.EqualTo(ScanAll(cells, blocked, position)),
                    $"iteration {iteration}: {width}x{height} at {position}");
                blocked.Dispose();
            }
        }

        private static int ScanAll(NavigationGridCells cells, NativeArray<byte> blocked, float2 position)
        {
            int start = cells.GetCell(position);
            if (blocked[start] == 0) return start;
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < blocked.Length; i++)
            {
                if (blocked[i] != 0) continue;
                float distance = math.distancesq(cells.GetNodePosition(i), position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }
    }
}
