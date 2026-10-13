using System;
using System.Collections.Generic;
using System.Linq;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class BattleSummary
    {
        public int Completed;
        public int WinsA;
        public int WinsB;
        public int Draws;
        public bool Cancelled;
        public readonly List<double> KillTimes = new List<double>();
        public double Mean => KillTimes.Count == 0 ? double.NaN : KillTimes.Average();
        public double Median => Percentile(0.5);
        public double P10 => Percentile(0.1);
        public double P90 => Percentile(0.9);
        public BattleResult FirstResult;

        public void Add(BattleResult result)
        {
            if (Completed == 0) FirstResult = result;
            Completed++;
            if (result.Winner == 0) WinsA++;
            else if (result.Winner == 1) WinsB++;
            else Draws++;
            if (result.Reason != "Timeout") KillTimes.Add(result.Duration);
        }

        public double Percentile(double fraction)
        {
            if (KillTimes.Count == 0) return double.NaN;
            double[] values = KillTimes.OrderBy(value => value).ToArray();
            double index = (values.Length - 1) * fraction;
            int lower = (int)Math.Floor(index), upper = (int)Math.Ceiling(index);
            return values[lower] + (values[upper] - values[lower]) * (index - lower);
        }
    }
}
