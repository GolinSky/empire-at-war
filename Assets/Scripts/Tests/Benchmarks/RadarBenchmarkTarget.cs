namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>Layout copy of <c>RadarTarget</c>; this assembly cannot reference game code.</summary>
    internal readonly struct RadarBenchmarkTarget
    {
        public readonly int OwnerSlot;
        public readonly bool IsDetectable;

        public RadarBenchmarkTarget(int ownerSlot, bool isDetectable)
        {
            OwnerSlot = ownerSlot;
            IsDetectable = isDetectable;
        }
    }
}
