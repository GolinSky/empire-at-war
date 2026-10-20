namespace EmpireAtWar.Services.VContainer
{
    public sealed class EntityLifetimeScope : AssetLifetimeScope
    {
        private EntityScopeOwnership _ownership;
        private bool _isOwned;

        protected override void Configure(global::VContainer.IContainerBuilder builder)
        {
            Assets = ((AssetLifetimeScope)Parent).Assets;
            // BaseFactory registers lifecycle after the entity's component injection callbacks.
        }

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
