using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.MiniMap;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.ViewComponents.Health
{
    public interface IHardPointProvider
    {
        HardPointType HardPointType { get; }
        int Id { get; }

        GameObject GameObject { get; }

        void SetId(int id);
    }
    public class HardPoint : MonoBehaviour, IHardPoint, INotifier<float>, IHardPointProvider
    {
        private const string EXPLOSION_VFX_PATH = "ExplosionVfx";

        private const float MAX_HEALTH = 1f;

        private readonly List<IObserver<float>> _observers = new List<IObserver<float>>();
        private ExplosionVfx _explosionVfx;

        private float _healthPercentage = MAX_HEALTH;

        [SerializeField] private bool spawnDestroyedExplosion = true;

        public event System.Action<ExplosionVfx> ExplosionSpawned;

        [field: SerializeField] public HardPointType HardPointType { get; private set; }
        [field: SerializeField] public int Id { get; private set; }
        public GameObject GameObject => gameObject;

        public Vector3 Position => Transform.position;
        public Transform Transform => transform;
        public bool IsDestroyed => _healthPercentage <= 0f;

        [Inject]
        protected IAssetService AssetService { get; }

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
            if (healthPercentage <= 0 && _explosionVfx == null && spawnDestroyedExplosion)
            {
                _explosionVfx = Instantiate(AssetService.LoadComponent<ExplosionVfx>(EXPLOSION_VFX_PATH), transform);
                _explosionVfx.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
                _explosionVfx.Play();
                ExplosionSpawned?.Invoke(_explosionVfx);
            }
        }
    }
}