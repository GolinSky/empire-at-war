namespace EmpireAtWar.Services.Tutorial
{
    public sealed class ImmediateHintCondition : IHintCondition
    {
        public bool IsMet => true;

        public void Dispose()
        {
        }
    }
}
