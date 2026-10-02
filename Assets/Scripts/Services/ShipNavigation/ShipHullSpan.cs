namespace EmpireAtWar.Services.ShipNavigation
{
    /// <summary>World-space vertical extent of a ship hull. Ships whose spans do not
    /// overlap pass over each other instead of keeping planar clearance.</summary>
    public readonly struct ShipHullSpan
    {
        public static readonly ShipHullSpan Unbounded =
            new ShipHullSpan(float.NegativeInfinity, float.PositiveInfinity);

        public ShipHullSpan(float bottom, float top)
        {
            Bottom = bottom;
            Top = top;
        }

        public float Bottom { get; }
        public float Top { get; }

        public bool Overlaps(ShipHullSpan other) => Bottom < other.Top && other.Bottom < Top;
    }
}
