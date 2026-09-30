namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipIcon
    {
        public TooltipIcon(string iconKey, string label) { IconKey = iconKey; Label = label; }
        public string IconKey { get; }
        public string Label { get; }
    }
}
