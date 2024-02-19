namespace EmpireAtWar.Views.ShipUi
{
    public interface INotifier<TValue>
    {
        void AddObserver(IObserver<TValue> observer);
        void RemoveObserver(IObserver<TValue> observer);
        
    }

    public interface IObserver<TValue>
    {
        void UpdateState(TValue value);
    }
}