using System.Collections.Generic;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyUnitLimitModel
    {
        private readonly Dictionary<string, int> _reservedCounts =
            new Dictionary<string, int>();

        public int CurrentUnitCapacity { get; private set; }

        public bool TryReserve(
            string unitId,
            int maxCount,
            int unitCapacity,
            int maxUnitCapacity)
        {
            if (!CanReserve(unitId, maxCount, unitCapacity, maxUnitCapacity))
            {
                return false;
            }

            int reservedCount = GetReservedCount(unitId);
            _reservedCounts[unitId] = reservedCount + 1;
            CurrentUnitCapacity += unitCapacity;
            return true;
        }

        public bool CanReserve(
            string unitId,
            int maxCount,
            int unitCapacity,
            int maxUnitCapacity)
        {
            return GetReservedCount(unitId) < maxCount &&
                CurrentUnitCapacity + unitCapacity <= maxUnitCapacity;
        }

        public bool CanReserve<TRequest>(
            string requestId,
            int maxCount,
            int unitCapacity,
            int maxUnitCapacity)
        {
            return CanReserve(
                $"{typeof(TRequest).FullName}:{requestId}",
                maxCount,
                unitCapacity,
                maxUnitCapacity);
        }

        public void Release(string unitId, int unitCapacity)
        {
            int reservedCount = GetReservedCount(unitId);
            if (reservedCount == 0)
            {
                return;
            }

            if (reservedCount == 1)
            {
                _reservedCounts.Remove(unitId);
            }
            else
            {
                _reservedCounts[unitId] = reservedCount - 1;
            }

            CurrentUnitCapacity = System.Math.Max(
                0,
                CurrentUnitCapacity - unitCapacity);
        }

        public int GetReservedCount(string unitId)
        {
            return _reservedCounts.TryGetValue(unitId, out int count) ? count : 0;
        }

        public int GetReservedCount<TRequest>(string requestId)
        {
            return GetReservedCount($"{typeof(TRequest).FullName}:{requestId}");
        }

        public void Reset()
        {
            _reservedCounts.Clear();
            CurrentUnitCapacity = 0;
        }
    }
}
