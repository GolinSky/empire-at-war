namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipHandle
    {
        public long Id { get; }

        public TooltipHandle(long id) => Id = id;
    }
}
