namespace EmpireAtWar.Services.Tooltip
{
    public interface ITooltipContentProvider
    {
        object Key { get; }
        object Source { get; }
        bool IsValid { get; }

        TooltipContent Build();
    }
}
