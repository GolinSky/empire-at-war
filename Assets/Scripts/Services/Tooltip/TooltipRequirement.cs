namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipRequirement
    {
        public string Text { get; }
        public bool IsMet { get; }

        public TooltipRequirement(string text, bool isMet) { Text = text; IsMet = isMet; }
    }
}
