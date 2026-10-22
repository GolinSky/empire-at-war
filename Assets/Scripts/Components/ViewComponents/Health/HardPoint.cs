using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.UnitExplosion;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

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
        private const float MAX_HEALTH = 1f;

        private readonly List<IObserver<float>> _observers = new List<IObserver<float>>();
        // A restored hardpoint (e.g. a reused squadron's fighter) explodes again when it is destroyed again.
        private bool _hasExploded;

        private float _healthPercentage = MAX_HEALTH;

        private bool _isInstalled = true;

        [SerializeField] private bool spawnDestroyedExplosion = true;
        [Tooltip("Surface position for destruction VFX. Combat targeting still uses the hardpoint transform.")]
        [SerializeField] private Transform destroyedVfxAnchor;
        [Tooltip("World scale of pooled destruction VFX; the flame diameter is approximately twice this value.")]
        [SerializeField, Min(0.01f)] private float destroyedVfxScale = 1f;
        [Tooltip("Upgrade level of the owner that installs this hardpoint; 1 = installed from the start.")]
        [SerializeField, Min(1)] private int unlockLevel = 1;

        [field: SerializeField] public HardPointType HardPointType { get; private set; }
        [field: SerializeField] public int Id { get; private set; }
        public GameObject GameObject => gameObject;
        public int UnlockLevel => unlockLevel;

        public Vector3 Position => Transform.position;
        public Transform Transform => transform;
        public bool IsDestroyed => _healthPercentage <= 0f;

        private IHardPointExplosionPool ExplosionPool { get; set; }

        private IFogVisibilityFacade FogVisibility { get; set; }

        [Inject]
        private void Construct(IHardPointExplosionPool explosionPool, IFogVisibilityFacade fogVisibility)
        {
            ExplosionPool = explosionPool;
            FogVisibility = fogVisibility;
        }

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

        protected virtual void OnDisable()
        {
            if (!_hasExploded) return;
            ExplosionPool.Stop(destroyedVfxAnchor);
            _hasExploded = false;
        }

        protected virtual void OnStateUpdated(float healthPercentage)
        {
            if (healthPercentage > 0)
            {
                if (_hasExploded) ExplosionPool.Stop(destroyedVfxAnchor);
                _hasExploded = false;
            }
            else if (_isInstalled && !_hasExploded && spawnDestroyedExplosion)
            {
                _hasExploded = true;
                ExplosionPool.Play(destroyedVfxAnchor, FogVisibility, destroyedVfxScale);
            }
        }
    }
}
