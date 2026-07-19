using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.MiningFacility;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ship;
using EmpireAtWar.SpaceStation;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;
using ShipEntity = EmpireAtWar.Ship.Ship;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.SceneContext.Skirmish
{
    public class GameUnitsInstaller: Installer
    {
        [Inject] private IRepository Repository { get; }

        public override void InstallBindings()
        {
            Container
                .BindFactory<PlayerType, ShipType, Vector3, ShipEntity, ShipFacadeFactory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<ShipInstaller>(GetPath<ShipInstaller>())
                .NonLazy();
    
            Container
                .BindFactory<PlayerType, FactionType, Vector3, SpaceStationEntity, SpaceStationFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<SpaceStationInstaller>(GetPath<SpaceStationInstaller>())
                .NonLazy();

            Container
                .BindFactory<PlayerType, MiningFacilityType, Vector3, MiningFacilityEntity, MiningFacilityFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<MiningFacilityInstaller>(GetPath<MiningFacilityInstaller>())
                .NonLazy();
            
            Container
                .BindFactory<PlayerType, DefendPlatformType, Vector3, DefendPlatform, DefendPlatformFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<DefendPlatformInstaller>(GetPath<DefendPlatformInstaller>())
                .NonLazy();
        }
        
        private GameObject GetPath<T>()
        {
            return Repository.Load<GameObject>(typeof(T).Name);
        }
    }
}
