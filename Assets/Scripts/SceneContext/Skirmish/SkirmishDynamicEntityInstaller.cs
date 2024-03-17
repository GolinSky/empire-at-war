using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Views.MiningFacility;
using EmpireAtWar.Views.Ship;
using EmpireAtWar.Views.SpaceStation;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class SkirmishDynamicEntityInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .BindFactory<PlayerType, ShipType, Vector3, ShipView, ShipFacadeFactory>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<ShipInstaller>()
                .NonLazy();

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
            
            //PlayerType,MiningFacilityType,Vector3,MiningFacilityView
        }
    }
}