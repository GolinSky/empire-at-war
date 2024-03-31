using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Views.DefendPlatform;
using EmpireAtWar.Views.MiningFacility;
using EmpireAtWar.Views.Ship;
using EmpireAtWar.Views.SpaceStation;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class SkirmishDynamicEntityInstaller : MonoInstaller
    {
        [Inject] private IRepository Repository { get; }
        
        public override void InstallBindings()
        {
            Container
                .BindFactory<PlayerType, ShipType, Vector3, ShipView, ShipFacadeFactory>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<ShipInstaller>()
                .NonLazy();
            // Container
            //     .BindFactory<PlayerType, FactionType, Vector3, SpaceStationView, SpaceStationViewFacade>()
            //     .FromFactory<SpaceStationViewFactory>();
            Container
                .BindFactory<PlayerType, FactionType, Vector3, SpaceStationView, SpaceStationViewFacade>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<StationInstaller>()
                .NonLazy();

            Container
                .BindFactory<PlayerType, MiningFacilityType, Vector3, MiningFacilityView, MiningFacilityFacadeFactory>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<MiningFacilityInstaller>()
                .NonLazy();
            
            Container
                .BindFactory<PlayerType, DefendPlatformType, Vector3, DefendPlatformView, DefendPlatformFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<DefendPlatformInstaller>(Repository.Load<GameObject>(nameof(DefendPlatformInstaller)))
                .NonLazy();

        }
    }
}