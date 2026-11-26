using System.Globalization;

namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipStat
    {
        private const string BONUS_COLOR = "#6DB8E2";

        public string Label { get; }
        public float Current { get; }
        public float? Max { get; }
        /// <summary>Research or other capacity bonus on top of the base value (<see cref="Max"/>, or <see cref="Current"/> without one).</summary>
        public float Bonus { get; }
        public string Format { get; }
        public string Display => Current.ToString(Format, CultureInfo.InvariantCulture) +
            (Max.HasValue ? " / " + Max.Value.ToString(Format, CultureInfo.InvariantCulture) : "") +
            (Bonus != 0f ? $" <color={BONUS_COLOR}>{(Bonus > 0f ? "+" : "")}{Bonus.ToString(Format, CultureInfo.InvariantCulture)}</color>" : "");

        public TooltipStat(string label, float current, string format = "0.#", float? max = null, float bonus = 0f)
        {
            Label = label;
            Current = current;
            Max = max;
            Bonus = bonus;
            Format = format;
        }
    }
}
