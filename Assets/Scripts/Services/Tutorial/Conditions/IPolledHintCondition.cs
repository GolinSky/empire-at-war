namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>A condition with no event to listen to; the hint service polls it every running frame.</summary>
    public interface IPolledHintCondition : IHintCondition
    {
        void Poll(float deltaTime);
    }
}
