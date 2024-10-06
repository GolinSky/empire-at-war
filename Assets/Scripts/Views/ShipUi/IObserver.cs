namespace EmpireAtWar
{
    public interface IObserver<TValue>
    {
        void UpdateState(TValue value);
    }
}