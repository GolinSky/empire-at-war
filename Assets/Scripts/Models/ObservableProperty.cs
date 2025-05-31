using System;

namespace EmpireAtWar.Models
{
    public interface IObservableProperty<TValue>
    {
         event Action<TValue> OnChanged; 
         TValue Value { get; }
    }

    public class ObservableProperty<TValue> : IObservableProperty<TValue>
    {
        public event Action<TValue> OnChanged;

        private TValue _value;
        
        public TValue Value
        {
            get => _value;
            set
            {
                _value = value;
                OnChanged?.Invoke(value);
            }
        }
    }
}