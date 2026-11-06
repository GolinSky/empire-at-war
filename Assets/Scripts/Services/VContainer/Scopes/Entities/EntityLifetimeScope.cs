using EmpireAtWar.Entities.BaseEntity;
using VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class EntityLifetimeScope : AssetLifetimeScope, IEntityScopeRelease, IEntityPoolActivation
    {
        private EntityScopeOwnership _ownership;
        private bool _isOwned;

        protected override void Configure(global::VContainer.IContainerBuilder builder)
        {
            Assets = ((AssetLifetimeScope)Parent).Assets;
            // LifetimeScope registers this instance by concrete type; expose its operations without taking ownership.
            builder.Register<IEntityScopeRelease>(_ => this, Lifetime.Transient);
            builder.Register<IEntityPoolActivation>(_ => this, Lifetime.Transient);
            // BaseFactory registers lifecycle after the entity's component injection callbacks.
        }

        void IEntityScopeRelease.Destroy(float delay) => Destroy(gameObject, delay);

        void IEntityPoolActivation.SetActive(bool active) => gameObject.SetActive(active);

        public void SetOwnership(EntityScopeOwnership ownership)
        {
            _ownership = ownership;
            _isOwned = true;
        }

        protected override void Awake()
        {
            autoRun = false;
            base.Awake();
        }

        protected override void OnDestroy()
        {
            try
            {
                base.OnDestroy();
            }
            finally
            {
                if (_isOwned) _ownership.Unregister(this);
            }
        }
    }
}
