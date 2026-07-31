using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Presenters.Cheats;
using EmpireAtWar.Presenters.Economy;
using EmpireAtWar.Presenters.Factions;
using EmpireAtWar.Presenters.Reinforcement;
using EmpireAtWar.SceneContext.Skirmish;
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
        [Inject] private IRepository Repository { get; }
        [Inject] private Zenject.SceneContext SceneContext { get; }

        public override void InstallBindings()
        {
            Container.Install<GameUnitsInstaller>();
            
            Container.BindScriptableObject<ReinforcementData>(Repository);
            Container.BindInterfacesAndSelfTo<ReinforcementModel>().AsSingle();
            Container.BindInterfacesNonLazyExt<ReinforcementService>();
            Container.BindInterfacesNonLazyExt<ReinforcementUiController>();

            Container.BindScriptableObject<PlayerFactionData>(Repository);
            Container.BindInterfacesAndSelfTo<PlayerFactionModel>().AsSingle();
            Container.BindInterfacesNonLazyExt<FactionService>();
            Container.BindInterfacesNonLazyExt<FactionUiController>();
            Container.BindInterfacesNonLazyExt<ShipBuildUiController>();
            
            Container.BindScriptableObject<EconomyData>(Repository);
            Container.BindInterfacesAndSelfTo<EconomyModel>().AsSingle();
            Container.BindInterfacesNonLazyExt<EconomyService>();
            Container.BindInterfacesNonLazyExt<EconomyUiController>();

            Container.BindInterfacesExt<CheatService>();
            Container
                .BindInterfacesAndSelfTo<CheatView>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName(nameof(CheatView))
                .AsSingle();
            Container.BindInterfacesNonLazyExt<CheatPresenter>();
            
            Container.BindInterfacesExt<PlayerService>();

            Container.BindInterfacesExt<PurchaseProcessor>();
        }
        
     
    }
}
