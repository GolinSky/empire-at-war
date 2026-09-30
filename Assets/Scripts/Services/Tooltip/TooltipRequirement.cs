namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipRequirement
    {
        public TooltipRequirement(string text, bool isMet) { Text = text; IsMet = isMet; }
        public string Text { get; }
        public bool IsMet { get; }
    }
}
