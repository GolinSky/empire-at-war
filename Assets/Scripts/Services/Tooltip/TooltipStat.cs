using System.Globalization;

namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipStat
    {
        public TooltipStat(string label, float current, float? max = null, string format = "0.#")
        {
            Label = label;
            Current = current;
            Max = max;
            Format = format;
        }

        public string Label { get; }
        public float Current { get; }
        public float? Max { get; }
        public string Format { get; }
        public string Display => Current.ToString(Format, CultureInfo.InvariantCulture) +
            (Max.HasValue ? " / " + Max.Value.ToString(Format, CultureInfo.InvariantCulture) : "");
    }
}
