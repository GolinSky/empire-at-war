namespace EmpireAtWar.Editor.Ship.HardPoints
{
    /// <summary>
    /// Yaw span in degrees in the hardpoint parent's frame (0 = bow, +90 = starboard). A span that crosses the
    /// rear seam has <see cref="Max"/> below <see cref="Min"/> and cannot be entered as one WeaponHardPoint range.
    /// </summary>
    public readonly struct YawArc
    {
        public const int FULL_CIRCLE = 360;

        public YawArc(int min, int max, int width, bool crossesRear)
        {
            Min = min;
            Max = max;
            Width = width;
            CrossesRear = crossesRear;
        }

        public int Min { get; }
        public int Max { get; }
        public int Width { get; }
        public bool CrossesRear { get; }
        public bool IsFull => Width >= FULL_CIRCLE;

        public override string ToString() => IsFull ? "-180..180 (full)" : $"{Min}..{Max} ({Width}°)";
    }
}
