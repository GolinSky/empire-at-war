using EmpireAtWar.Lifecycle;
using EmpireAtWar.Mvc;
using EmpireAtWar.Repository;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public class AssetLifetimeScope : LifetimeScope
    {
        private CachedAssetService _ownedAssets;

        public IAssetService Assets { get; protected set; }

        protected override void Configure(IContainerBuilder builder)
        {
            Assets = ((AssetLifetimeScope)Parent).Assets;
            builder.RegisterLifecycle();
        }

        // Child scopes inherit these assets; they are released when this scope is destroyed.
        protected void OwnAssets(CachedAssetService assets)
        {
            _ownedAssets = assets;
            Assets = assets;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // Released after container disposal, which destroys this scope's child scopes, pools and instances.
            if (_ownedAssets != null) _ownedAssets.Dispose();
        }
    }
}
