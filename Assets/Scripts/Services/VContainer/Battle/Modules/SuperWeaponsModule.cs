using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.SuperWeapons.Controller;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.Saves;
using global::VContainer;
using UnityEngine;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>Super weapon firing and controls. Charge state, targeting and the catalog stay registered by the
    /// battle and player scopes; with this module off the roster offers no super weapons, so they stay empty.</summary>
    public sealed class SuperWeaponsModule : IBattleModule
    {
        private readonly IAssetService _assets;
        public BattleFeatures Feature => BattleFeatures.SuperWeapons;

        public SuperWeaponsModule(IAssetService assets) => _assets = assets;

        public void ConfigureBattle(IContainerBuilder builder)
        {
            AssetRegistrations.RegisterShared<SuperWeaponData>(builder, _assets);
            builder.Register<SuperWeaponFireService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SuperWeaponCombatSaveSource>(Lifetime.Singleton).As<IBattleSaveSource>();
        }

        public void ConfigureDisabled(IContainerBuilder builder) { }

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot)
        {
            if (!player.Slot.IsAi)
                builder.Register<SuperWeaponPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            else if (player.Has(BattleFeatures.Economy))
                builder.Register<EnemySuperWeaponController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
