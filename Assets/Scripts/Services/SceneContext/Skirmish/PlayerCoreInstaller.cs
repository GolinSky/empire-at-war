using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Presenters.Factions;
using EmpireAtWar.Presenters.Reinforcement;
using EmpireAtWar.SceneContext.Skirmish;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Reinforcement;
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
            
            Container.BindModel<EconomyModel>(Repository);
            Container.BindInterfacesNonLazyExt<EconomyController>();
            
            Container.BindInterfacesExt<PlayerService>();

            Container.BindInterfacesExt<PurchaseProcessor>();
        }
        
     
    }
}
