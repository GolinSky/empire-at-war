namespace EmpireAtWar.Services.Timing
{
    public interface ISlowTickService
    {
        void Add(ISlowTickable tickable);

        void Remove(ISlowTickable tickable);
    }
}
