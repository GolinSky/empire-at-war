using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
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

        private bool _isInstalled = true;

        [SerializeField] private bool spawnDestroyedExplosion = true;
        [Tooltip("Upgrade level of the owner that installs this hardpoint; 1 = installed from the start.")]
        [SerializeField, Min(1)] private int unlockLevel = 1;

        public event System.Action<ExplosionVfx> ExplosionSpawned;

        [field: SerializeField] public HardPointType HardPointType { get; private set; }
        [field: SerializeField] public int Id { get; private set; }
        public GameObject GameObject => gameObject;
        public int UnlockLevel => unlockLevel;

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

        public void SetInstalled(bool installed)
        {
            _isInstalled = installed;
            gameObject.SetActive(installed);
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

        public virtual bool TryGetWeaponType(out WeaponType weaponType)
        {
            weaponType = default;
            return false;
        }

        protected virtual void OnInit(){}

        protected virtual void OnRelease(){}

        protected virtual void OnStateUpdated(float healthPercentage)
        {
            if (_isInstalled && healthPercentage <= 0 && _explosionVfx == null && spawnDestroyedExplosion)
            {
                _explosionVfx = Instantiate(AssetService.LoadComponent<ExplosionVfx>(EXPLOSION_VFX_PATH), transform);
                _explosionVfx.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
                _explosionVfx.Play();
                ExplosionSpawned?.Invoke(_explosionVfx);
            }
        }
    }
}