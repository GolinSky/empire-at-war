namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Completion for information-only hints: only Dismiss closes them.</summary>
    public sealed class NeverHintCondition : IHintCondition
    {
        public bool IsMet => false;

        public void Dispose()
        {
        }
    }
}
