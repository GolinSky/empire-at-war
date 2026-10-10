using System.Collections.Generic;

namespace EmpireAtWar.Utils
{
    /// <summary>Holds one value and pushes it to every observer on subscribe and on change.</summary>
    public sealed class ReplayNotifier<TValue> : INotifier<TValue>
    {
        private readonly List<IObserver<TValue>> _observers = new List<IObserver<TValue>>();

        public TValue Value { get; private set; }

        public ReplayNotifier(TValue value)
        {
            Value = value;
        }

        public void Set(TValue value)
        {
            if (EqualityComparer<TValue>.Default.Equals(Value, value))
            {
                return;
            }

            Value = value;
            // Observers may unsubscribe while handling the change.
            foreach (IObserver<TValue> observer in _observers.ToArray())
            {
                observer.UpdateState(value);
            }
        }

        public void AddObserver(IObserver<TValue> observer)
        {
            if (_observers.Contains(observer))
            {
                return;
            }

            _observers.Add(observer);
            observer.UpdateState(Value);
        }

        public void RemoveObserver(IObserver<TValue> observer)
        {
            _observers.Remove(observer);
        }
    }
}
