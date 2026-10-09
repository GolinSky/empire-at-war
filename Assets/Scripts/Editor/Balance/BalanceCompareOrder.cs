using System;
using System.Globalization;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceCompareOrder
    {
        public static void Sort(BalanceRegistration registry, BalanceWindowState state)
        {
            if (state.CompareSort == BalanceCompareSort.Manual) return;
            var units = state.Pins.Select(id => registry.Units.FirstOrDefault(unit => unit.Id == id)).Where(unit => unit != null);
            state.Pins = units.OrderBy(unit => state.CompareSort switch
            {
                BalanceCompareSort.Price => Value(unit, registry, state, "Price"),
                BalanceCompareSort.HeightLevel => Value(unit, registry, state, "HeightTier", "Height"),
                BalanceCompareSort.AvailabilityLevel => Value(unit, registry, state, "AvailableLevel"),
                BalanceCompareSort.Class => ClassRank(unit),
                _ => throw new InvalidOperationException("Unknown comparison order: " + state.CompareSort)
            }).Select(unit => unit.Id).ToList();
        }

        public static bool Move(BalanceWindowState state, string source, string target, bool after)
        {
            if (source == target) return false;
            int from = state.Pins.IndexOf(source);
            int to = state.Pins.IndexOf(target);
            if (from < 0 || to < 0) return false;
            int destination = to + (after ? 1 : 0) - (from < to ? 1 : 0);
            if (destination == from) return false;
            state.Pins.RemoveAt(from);
            state.Pins.Insert(destination, source);
            state.CompareSort = BalanceCompareSort.Manual;
            return true;
        }

        private static double Value(BalanceUnit unit, BalanceRegistration registry, BalanceWindowState state, params string[] stats)
        {
            foreach (string stat in stats)
                if (unit.Fields.TryGetValue(stat, out string key))
                    return double.Parse(registry.Fields[key].DraftValue(state.Draft), CultureInfo.InvariantCulture);
            return double.PositiveInfinity;
        }

        private static int ClassRank(BalanceUnit unit)
        {
            if (unit.Kind == BalanceUnitKind.Squadron) return 0;
            if (!Enum.TryParse(unit.Class, out ShipClass shipClass)) return 6;
            return shipClass switch
            {
                ShipClass.Fighter or ShipClass.Bomber or ShipClass.Interceptor => 0,
                ShipClass.Corvette => 1,
                ShipClass.Frigate => 2,
                ShipClass.Cruiser => 3,
                ShipClass.Capital => 4,
                ShipClass.HeavyCapital => 5,
                _ => 6
            };
        }
    }
}
