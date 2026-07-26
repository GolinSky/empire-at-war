using System.Collections.Generic;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyUnitLimitModel
    {
        private readonly Dictionary<string, int> _reservedCounts = new Dictionary<string, int>();

        public int CurrentUnitCapacity { get; private set; }

        public bool TryReserve(
            string unitId,
            int maxCount,
            int unitCapacity,
            int maxUnitCapacity)
        {
            int reservedCount = GetReservedCount(unitId);
            if (reservedCount >= maxCount ||
                CurrentUnitCapacity + unitCapacity > maxUnitCapacity)
            {
                return false;
            }

            _reservedCounts[unitId] = reservedCount + 1;
            CurrentUnitCapacity += unitCapacity;
            return true;
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

            CurrentUnitCapacity = System.Math.Max(0, CurrentUnitCapacity - unitCapacity);
        }

        public int GetReservedCount(string unitId)
        {
            return _reservedCounts.TryGetValue(unitId, out int count) ? count : 0;
        }

        public void Reset()
        {
            _reservedCounts.Clear();
            CurrentUnitCapacity = 0;
        }
    }
}
