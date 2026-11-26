using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyUnitLimitModel : Model
    {
        /// <summary>
        /// Like a human, the AI may buy past the fleet cap into a held reserve, up to this many fleets beyond it
        /// (user 2026-11-26); reserve units deploy only into live capacity.
        /// </summary>
        public const int RESERVE_FLEETS = 1;

        private readonly Dictionary<UnitLimitKey, int> _reservedCounts =
            new Dictionary<UnitLimitKey, int>();
        private readonly HashSet<UnitRequest> _deployed = new HashSet<UnitRequest>();

        /// <summary>Population of every paid combat unit: in production, held in reserve or on the map.</summary>
        public int CurrentUnitCapacity { get; private set; }
        /// <summary>Population of combat units on the map; only this is held to the fleet cap.</summary>
        public int DeployedUnitCapacity { get; private set; }
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

        public void Release(UnitRequest request)
        {
            int capacity = GetCapacity(request.FactionData, IsCombatUnit(request));
            if (_deployed.Remove(request)) DeployedUnitCapacity = System.Math.Max(0, DeployedUnitCapacity - capacity);
            Release(UnitLimitKey.From(request), capacity);
        }

        /// <summary>
        /// The human purchase limit (<c>PlayerFactionModel.IsUnitLimitReached</c>): a ship or squadron type has none
        /// except a unique one (hero, dreadnought), which exists once while queued, held or alive; a structure is limited to its
        /// <see cref="FactionData.MaxCount"/> queued or built; research and upgrades to <see cref="FactionData.MaxCount"/>
        /// in production, as the human queue limit.
        /// </summary>
        /// <param name="isCombatUnit">Ship or squadron.</param>
        public static int GetPurchaseLimit(FactionData data, bool isCombatUnit) =>
            !isCombatUnit ? data.MaxCount : data.IsUnique ? 1 : int.MaxValue;

        /// <summary>Total population the AI may pay for: the fleet cap plus <see cref="RESERVE_FLEETS"/> fleets.</summary>
        public static int GetPurchaseCapacityLimit(int maxUnitCapacity) => maxUnitCapacity * (1 + RESERVE_FLEETS);

        /// <summary>The unit landed on the map: its population now counts against the live fleet cap.</summary>
        public void MarkDeployed(UnitRequest request)
        {
            if (!_deployed.Add(request)) return;
            DeployedUnitCapacity += GetCapacity(request.FactionData, IsCombatUnit(request));
        }

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
            _deployed.Clear();
            CurrentUnitCapacity = 0;
            DeployedUnitCapacity = 0;
            ShipOrdersCount = 0;
            ReleaseVersion = 0;
        }
    }
}
