namespace EmpireAtWar
{
    public interface INotifier<TValue>
    {
        void AddObserver(IObserver<TValue> observer);
        void RemoveObserver(IObserver<TValue> observer);
        
    }
}