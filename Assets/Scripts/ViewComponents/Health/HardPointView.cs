using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public interface IHardPointProvider
    {
        void SetId(int id);
        HardPointType HardPointType { get; }
        int Id { get; }
    }
    public class HardPointView : MonoBehaviour, IHardPointView, INotifier<float>, IHardPointProvider
    {
        private const float MAX_HEALTH = 1f;
        [field: SerializeField] public HardPointType HardPointType { get; private set; }
        [field: SerializeField] public int Id { get; private set; }

        private List<IObserver<float>> observers = new List<IObserver<float>>();
        private float _healthPercentage = MAX_HEALTH;

        public Vector3 Position => transform.position;
        public bool IsDestroyed => _healthPercentage <= 0f;


        public void UpdateData(float healthPercentage)
        {
            _healthPercentage = healthPercentage;
            foreach (IObserver<float> observer in observers)
            {
                observer.UpdateState(healthPercentage);
            }

            OnStateUpdated(healthPercentage);
        }

        void INotifier<float>.AddObserver(IObserver<float> observer)
        {
            observers.Add(observer);
        }

        void INotifier<float>.RemoveObserver(IObserver<float> observer)
        {
            observers.Remove(observer);
        }

        void IHardPointProvider.SetId(int id)
        {
            Id = id;
        }
        
        
        protected virtual void OnInit(){}
        protected virtual void OnRelease(){}
        
        protected virtual void OnStateUpdated(float healthPercentage){}
    }
}