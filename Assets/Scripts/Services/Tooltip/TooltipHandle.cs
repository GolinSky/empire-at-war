namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipHandle
    {
        public TooltipHandle(long id) => Id = id;
        public long Id { get; }
    }
}
