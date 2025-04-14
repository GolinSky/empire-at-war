using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.SceneContext.Skirmish;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Ui.Base;
using LightWeightFramework.Components.Repository;
using UnityEngine;
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
            
            Container.BindModel<ReinforcementModel>(Repository);
            Container.BindInterfacesNonLazyExt<ReinforcementController>();

            Container.BindModel<PlayerFactionModel>(Repository);
            Container.BindInterfacesNonLazyExt<FactionController>();
            
            Container.BindModel<EconomyModel>(Repository);
            Container.BindInterfacesNonLazyExt<EconomyController>();
            
            Container.BindInterfacesExt<PlayerService>();
          
            //todo: rebind it here - bind it in scene context
            Container
                .BindFactory<UiType, Transform, BaseUi, UiFacade>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<UiInstaller>();
            
            Container.BindInterfacesExt<UiService>();
            Container.BindInterfacesExt<PurchaseProcessor>();
        }
        
     
    }
}