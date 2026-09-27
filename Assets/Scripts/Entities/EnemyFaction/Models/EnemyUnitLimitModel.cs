using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyUnitLimitModel : PureModel
    {
        private readonly Dictionary<UnitLimitKey, int> _reservedCounts =
            new Dictionary<UnitLimitKey, int>();

        public int CurrentUnitCapacity { get; private set; }
        public int ShipOrdersCount { get; private set; }
        public int ReleaseVersion { get; private set; }

        public void RecordShipOrder()
        {
            ShipOrdersCount++;
        }

        public void CancelShipOrder()
        {
            ShipOrdersCount--;
        }

        public bool TryReserve(
            UnitLimitKey unitId,
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
            UnitLimitKey unitId,
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
                UnitLimitKey.For<TRequest>(requestId),
                maxCount,
                unitCapacity,
                maxUnitCapacity);
        }

        public void Release(UnitLimitKey unitId, int unitCapacity)
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
            ReleaseVersion++;
        }

        public int GetReservedCount(UnitLimitKey unitId)
        {
            return _reservedCounts.TryGetValue(unitId, out int count) ? count : 0;
        }

        public int GetReservedCount<TRequest>(string requestId)
        {
            return GetReservedCount(UnitLimitKey.For<TRequest>(requestId));
        }

        public void Reset()
        {
            _reservedCounts.Clear();
            CurrentUnitCapacity = 0;
            ShipOrdersCount = 0;
            ReleaseVersion = 0;
        }
    }
}
