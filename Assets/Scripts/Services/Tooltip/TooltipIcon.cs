namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipIcon
    {
        public string IconKey { get; }
        public string Label { get; }

        public TooltipIcon(string iconKey, string label) { IconKey = iconKey; Label = label; }
    }
}
