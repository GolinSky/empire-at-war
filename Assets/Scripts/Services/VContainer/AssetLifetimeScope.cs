using EmpireAtWar.Mvc;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public class AssetLifetimeScope : LifetimeScope
    {
        public IAssetService Assets { get; protected set; }

        protected override void Configure(IContainerBuilder builder)
        {
            Assets = ((AssetLifetimeScope)Parent).Assets;
        }
    }
}
