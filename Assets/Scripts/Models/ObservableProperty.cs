using System;

namespace EmpireAtWar.Models
{
    public interface IObservableProperty<TValue>
    {
        event Action<TValue> OnChanged;
        TValue Value { get; }
        bool HasValue { get; }
    }

    public class ObservableProperty<TValue> : IObservableProperty<TValue>
    {
        public event Action<TValue> OnChanged;

        private TValue _value;
        public bool HasValue { get; private set; }

        public TValue Value
        {
            get => _value;
            set
            {
                _value = value;
                HasValue = true;
                OnChanged?.Invoke(value);
            }
        }
    }
}