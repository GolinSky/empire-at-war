namespace EmpireAtWar.Services.Tooltip
{
    public readonly struct TooltipAnchor
    {
        public TooltipAnchor(TooltipAnchorKind kind, float x, float y, float width = 0f, float height = 0f)
        {
            Kind = kind;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public TooltipAnchorKind Kind { get; }
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
    }
}
