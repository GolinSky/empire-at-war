using System.Globalization;

namespace EmpireAtWar.Editor.Balance
{
    public readonly struct BalanceDpsEstimate
    {
        public const string NOT_ARMED = "N/A";
        public const string INVALID_DRAFT = "Invalid draft";

        public double Value { get; }
        public string Problem { get; }
        public bool HasValue => Problem.Length == 0;

        private BalanceDpsEstimate(double value, string problem)
        {
            Value = value;
            Problem = problem;
        }

        public static BalanceDpsEstimate Of(double value) => new BalanceDpsEstimate(value, "");

        public static BalanceDpsEstimate Unavailable(string problem) => new BalanceDpsEstimate(0, problem);

        public override string ToString() => HasValue ? Value.ToString("N1", CultureInfo.InvariantCulture) : Problem;
    }
}
