using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Extentions;
using EmpireAtWar.MiningFacility;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Ship;
using EmpireAtWar.SpaceStation;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.DefendPlatform;
using EmpireAtWar.Views.MiningFacility;
using EmpireAtWar.Views.SpaceStation;
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

            Container
                .BindFactory<PlayerType, ShipType, Vector3, ShipView, ShipFacadeFactory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<ShipInstaller>(GetPath<ShipInstaller>())
                .NonLazy();
    
            Container
                .BindFactory<PlayerType, FactionType, Vector3, SpaceStationView, SpaceStationViewFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<SpaceStationInstaller>(GetPath<SpaceStationInstaller>())
                .NonLazy();

            Container
                .BindFactory<PlayerType, MiningFacilityType, Vector3, MiningFacilityView, MiningFacilityFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<MiningFacilityInstaller>(GetPath<MiningFacilityInstaller>())
                .NonLazy();
            
            Container
                .BindFactory<PlayerType, DefendPlatformType, Vector3, DefendPlatformView, DefendPlatformFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<DefendPlatformInstaller>(GetPath<DefendPlatformInstaller>())
                .NonLazy();
       
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
        
        private GameObject GetPath<T>()
        {
            return Repository.Load<GameObject>(typeof(T).Name);
        }
    }
}