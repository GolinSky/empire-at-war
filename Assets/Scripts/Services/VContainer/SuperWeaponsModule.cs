using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.SuperWeapons.Controller;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.SuperWeapons;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class SuperWeaponsModule : IBattleModule
    {
        private readonly IAssetService _assets;
        public BattleFeatures Feature => BattleFeatures.SuperWeapons;

        public SuperWeaponsModule(IAssetService assets) => _assets = assets;

        public void ConfigureBattle(IContainerBuilder builder)
        {
            AssetRegistrations.RegisterShared<SuperWeaponData>(builder, _assets);
            AssetRegistrations.RegisterShared<SuperWeaponCatalog>(builder, _assets);
            builder.Register<SuperWeaponTargetingModel>(Lifetime.Singleton);
            builder.Register<SuperWeaponOriginRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SuperWeaponFireService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player)
        {
            builder.Register<SuperWeaponModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (player.Role == PlayerRole.Human)
                builder.Register<SuperWeaponPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            else if ((player.Features & BattleFeatures.Economy) != 0)
                builder.Register<EnemySuperWeaponController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
