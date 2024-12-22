using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

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
        private const string EXPLOSION_VFX_PATH = "ExplosionVfx";
        private const float MAX_HEALTH = 1f;
        [field: SerializeField] public HardPointType HardPointType { get; private set; }
        [field: SerializeField] public int Id { get; private set; }

        private readonly List<IObserver<float>> _observers = new List<IObserver<float>>();
        private ParticleSystem _explosionVfx;

        private float _healthPercentage = MAX_HEALTH;

        public Vector3 Position => transform.position;
        public bool IsDestroyed => _healthPercentage <= 0f;

        [Inject]
        protected IRepository Repository { get; }

        public void UpdateData(float healthPercentage)
        {
            _healthPercentage = healthPercentage;
            foreach (IObserver<float> observer in _observers)
            {
                observer.UpdateState(healthPercentage);
            }

            OnStateUpdated(healthPercentage);
        }

        void INotifier<float>.AddObserver(IObserver<float> observer)
        {
            _observers.Add(observer);
        }

        void INotifier<float>.RemoveObserver(IObserver<float> observer)
        {
            _observers.Remove(observer);
        }

        void IHardPointProvider.SetId(int id)
        {
            Id = id;
        }
        
        protected virtual void OnInit(){}
        protected virtual void OnRelease(){}

        protected virtual void OnStateUpdated(float healthPercentage)
        {
            if (healthPercentage <= 0 && _explosionVfx == null)
            {
                _explosionVfx = Instantiate(Repository.LoadComponent<ParticleSystem>(EXPLOSION_VFX_PATH), transform);
                _explosionVfx.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
                _explosionVfx.Play();
            }
        }
    }
}