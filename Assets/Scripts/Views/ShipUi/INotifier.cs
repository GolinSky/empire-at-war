namespace EmpireAtWar.Views.ShipUi
{
    public interface INotifier<TValue>
    {
        void Add(IObserver<TValue> observer);
        void Remove(IObserver<TValue> observer);
        
    }

    public interface IObserver<TValue>
    {
        void UpdateState(TValue value);
    }
}