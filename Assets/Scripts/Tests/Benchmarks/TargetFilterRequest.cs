namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>The filter fields of <c>WeaponTargetSelectionJob.Input</c>.</summary>
    internal struct TargetFilterRequest
    {
        public int CandidateStart;
        public int CandidateCount;
        public int GroupStart;
        public int StrikecraftOnly;
        public int ShipsOnly;
        // A TargetFilterFlags.PREFERENCE_* value.
        public int Preference;
    }
}
