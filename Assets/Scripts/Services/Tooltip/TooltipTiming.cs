namespace EmpireAtWar.Services.Tooltip
{
    public sealed class TooltipTiming
    {
        public float ShowDelay { get; }
        public float RefreshInterval { get; }

        public TooltipTiming(float showDelay, float refreshInterval)
        { ShowDelay = showDelay; RefreshInterval = refreshInterval; }
    }
}
