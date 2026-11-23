using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyUnitLimitModel : Model
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

        /// <summary>
        /// Reserves a purchase under the human limits (D6, W69): see <see cref="GetPurchaseLimit"/> and
        /// <see cref="GetCapacity"/>. The AI takes the population slot at purchase, not at deployment.
        /// </summary>
        public bool TryReserve(UnitRequest request, int maxUnitCapacity)
        {
            bool isCombatUnit = IsCombatUnit(request);
            return TryReserve(UnitLimitKey.From(request), GetPurchaseLimit(request.FactionData, isCombatUnit),
                GetCapacity(request.FactionData, isCombatUnit), maxUnitCapacity);
        }

        /// <summary>Takes a reservation for a unit restored from a save, which already passed the limits once.</summary>
        public void Adopt(UnitRequest request) =>
            Adopt(UnitLimitKey.From(request), GetCapacity(request.FactionData, IsCombatUnit(request)));

        public void Release(UnitRequest request) =>
            Release(UnitLimitKey.From(request), GetCapacity(request.FactionData, IsCombatUnit(request)));

        /// <summary>
        /// The human purchase limit (<c>PlayerFactionModel.IsUnitLimitReached</c>): a ship or squadron type has none
        /// except a hero, which exists once while queued, held or alive; a structure is limited to its
        /// <see cref="FactionData.MaxCount"/> queued or built; research and upgrades to <see cref="FactionData.MaxCount"/>
        /// in production, as the human queue limit.
        /// </summary>
        /// <param name="isCombatUnit">Ship or squadron.</param>
        public static int GetPurchaseLimit(FactionData data, bool isCombatUnit) =>
            !isCombatUnit ? data.MaxCount : data.IsHero ? 1 : int.MaxValue;

        /// <summary>Only ships and squadrons use population, as for a human (<c>ReinforcementModel.UsesCapacity</c>).</summary>
        public static int GetCapacity(FactionData data, bool isCombatUnit) => isCombatUnit ? data.UnitCapacity : 0;

        private static bool IsCombatUnit(UnitRequest request) => ReinforcementModel.UsesCapacity(request);

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

        /// <summary>Takes a reservation for a unit restored from a save, which already passed the limits once.</summary>
        public void Adopt(UnitLimitKey unitId, int unitCapacity)
        {
            _reservedCounts[unitId] = GetReservedCount(unitId) + 1;
            CurrentUnitCapacity += unitCapacity;
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
