using System;
using System.Collections.Generic;
using System.Diagnostics;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Ships
{
    internal sealed class ShipFacingSolveScheduler : ILateTickable
    {
        private const int MAX_REFRESHES_PER_FRAME = 2;
        private static readonly long _budgetTicks = Stopwatch.Frequency / 1000;
        private readonly Queue<Action> _refreshes = new Queue<Action>();

        public void Request(Action refresh) => _refreshes.Enqueue(refresh);

        public void LateTick()
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < MAX_REFRESHES_PER_FRAME && _refreshes.Count > 0; i++)
            {
                if (i > 0 && Stopwatch.GetTimestamp() - start >= _budgetTicks) break;
                _refreshes.Dequeue()();
            }
        }
    }
}
