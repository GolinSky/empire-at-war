using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceCompareOrder
    {
        public static void Sort(BalanceRegistration registry, BalanceWindowState state)
        {
            if (state.CompareSort == BalanceCompareSort.Manual) return;
            var units = state.Pins.Select(id => registry.Units.FirstOrDefault(unit => unit.Id == id)).Where(unit => unit != null).ToList();
            if (state.CompareSort == BalanceCompareSort.Dps)
            {
                using BalanceDpsEstimator estimator = new BalanceDpsEstimator(registry);
                state.Pins = Order(units, unit => Dps(estimator.Estimate(unit, state.Draft)), state.CompareDescending);
                return;
            }
            state.Pins = Order(units, unit => state.CompareSort switch
            {
                BalanceCompareSort.Price => Value(unit, registry, state, "Price"),
                BalanceCompareSort.HeightLevel => Value(unit, registry, state, "HeightTier", "Height"),
                BalanceCompareSort.AvailabilityLevel => Value(unit, registry, state, "AvailableLevel"),
                BalanceCompareSort.Class => ClassRank(unit),
                BalanceCompareSort.Health => Value(unit, registry, state, "Hull", "MemberHull"),
                BalanceCompareSort.Shield => Value(unit, registry, state, "Shields", "MemberShields"),
                BalanceCompareSort.BuildTime => Value(unit, registry, state, "BuildTime"),
                BalanceCompareSort.Speed => Value(unit, registry, state, "Speed", "CruiseSpeed"),
                _ => throw new InvalidOperationException("Unknown comparison order: " + state.CompareSort)
            }, state.CompareDescending);
        }

        // Units without the stat (positive infinity) stay last in both directions.
        private static List<string> Order(List<BalanceUnit> units, Func<BalanceUnit, double> key, bool descending)
        {
            var keyed = units.Select(unit => (unit, value: key(unit))).OrderBy(entry => double.IsPositiveInfinity(entry.value));
            var ordered = descending ? keyed.ThenByDescending(entry => entry.value) : keyed.ThenBy(entry => entry.value);
            return ordered.Select(entry => entry.unit.Id).ToList();
        }

        private static double Dps(BalanceDpsEstimate estimate) => estimate.HasValue ? estimate.Value : double.PositiveInfinity;

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
            if (!Enum.TryParse(unit.Class, out ShipClass shipClass)) return 8;
            return shipClass switch
            {
                ShipClass.Fighter or ShipClass.Bomber or ShipClass.Interceptor or ShipClass.HeroStrikecraft => 0,
                ShipClass.Corvette => 1,
                ShipClass.Frigate => 2,
                ShipClass.Cruiser => 3,
                ShipClass.HeavyCruiser => 4,
                ShipClass.Capital => 5,
                ShipClass.BattleCruiser => 6,
                ShipClass.Dreadnought => 7,
                _ => 8
            };
        }
    }
}
