using System.Globalization;

namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipStat
    {
        public string Label { get; }
        public float Current { get; }
        public float? Max { get; }
        public string Format { get; }
        public string Display => Current.ToString(Format, CultureInfo.InvariantCulture) +
            (Max.HasValue ? " / " + Max.Value.ToString(Format, CultureInfo.InvariantCulture) : "");

        public TooltipStat(string label, float current, string format = "0.#", float? max = null)
        {
            Label = label;
            Current = current;
            Max = max;
            Format = format;
        }
    }
}
