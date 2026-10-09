using System.Linq;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.Heroes;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.SuperWeapons.Controller;
using EmpireAtWar.Extensions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Presenters.Cheats;
using EmpireAtWar.Presenters.Economy;
using EmpireAtWar.Presenters.Factions;
using EmpireAtWar.Presenters.Reinforcement;
using EmpireAtWar.SceneContext.Skirmish;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Views.Cheats;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar
{
    public class PlayerCoreInstaller : MonoInstaller
    {
        [Inject] private IAssetService AssetService { get; }
        [Inject] private ILocalPlayer LocalPlayer { get; }
        [Inject] private FactionCatalog FactionCatalog { get; }

        public override void InstallBindings()
        {
            // Everything in this context, including the units it spawns, belongs to the local human.
            Container.BindInstance(LocalPlayer.Slot);
            Container.Install<GameUnitsInstaller>();
            
            Container.BindScriptableObject<ReinforcementData>(AssetService);
            Container.BindInterfacesAndSelfTo<ReinforcementModel>().AsSingle();
            Container.Bind<ReinforcementPreviewFactory>().AsSingle();
            Container.Bind<ReinforcementPlacementFactory>().AsSingle();
            Container.BindInterfacesNonLazyExt<ReinforcementService>();
            Container.ParentContainers.Single()
                .Bind<IReinforcementService>()
                .FromSubContainerResolve()
                .ByInstance(Container)
                .AsSingle();
            Container.BindInterfacesNonLazyExt<ReinforcementUiController>();

            Container
                .Bind<FactionRoster>()
                .AsSingle()
                .WithArguments(FactionCatalog.Get(LocalPlayer.Slot.Faction));
            Container.BindInterfacesAndSelfTo<PlayerFactionModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<FactionResearchModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<SuperWeaponModel>().AsSingle();
            Container.BindInterfacesNonLazyExt<SuperWeaponPresenter>();
            Container.BindInterfacesNonLazyExt<FactionService>();
            Container.BindInterfacesNonLazyExt<FactionUiController>();
            Container.BindInterfacesNonLazyExt<HeroUiController>();
            Container.BindInterfacesNonLazyExt<ShipBuildUiController>();
            
            Container.BindScriptableObject<EconomyData>(AssetService);
            Container.BindInterfacesAndSelfTo<EconomyModel>().AsSingle();
            Container.BindInterfacesNonLazyExt<EconomyService>();
            Container.BindInterfacesNonLazyExt<EconomyUiController>();

            // Registers itself in the scene-wide player registry so capture sites can build for the player.
            Container.BindInterfacesTo<SiteFacilityBuilder>().AsSingle().NonLazy();

            Container.BindInterfacesExt<CheatService>();
            Container
                .BindInterfacesAndSelfTo<CheatView>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName(nameof(CheatView))
                .AsSingle();
            Container.BindInterfacesNonLazyExt<CheatPresenter>();
            
            Container.BindInterfacesExt<PlayerService>();
        }
        
     
    }
}
