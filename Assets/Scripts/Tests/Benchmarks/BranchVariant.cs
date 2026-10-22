namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>How a kernel expresses its per-element decision (Burst_Jobs_Optimization_Plan, Phase 1).</summary>
    public enum BranchVariant
    {
        If = 0,
        Ternary = 1,
        Select = 2,
    }
}
