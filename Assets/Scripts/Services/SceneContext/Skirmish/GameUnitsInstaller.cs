using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.MiningFacility;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ship;
using EmpireAtWar.SpaceStation;
using EmpireAtWar.Mvc;
using EmpireAtWar.Extensions;
using EmpireAtWar.Services.Squadrons;
using UnityEngine;
using Zenject;
using ShipEntity = EmpireAtWar.Ship.Ship;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.SceneContext.Skirmish
{
    public class GameUnitsInstaller: Installer
    {
        [Inject] private IAssetService AssetService { get; }

        public override void InstallBindings()
        {
            Container
                .BindFactory<PlayerId, ShipType, Vector3, ShipEntity, ShipFactory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<ShipInstaller>(GetPath<ShipInstaller>())
                .NonLazy();

            Container
                .BindFactory<PlayerId, SquadronType, Vector3, Quaternion, Squadron, SquadronFactory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<SquadronInstaller>(GetPath<SquadronInstaller>())
                .NonLazy();
            // Per player like the factory it pools: pools are keyed by owner, and hangars and reinforcements
            // resolve their spawner from their own player's container.
            Container.BindInterfacesExt<SquadronPool>();

            Container
                .BindFactory<PlayerId, FactionType, Vector3, SpaceStationEntity, SpaceStationFactory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<SpaceStationInstaller>(GetPath<SpaceStationInstaller>())
                .NonLazy();

            Container
                .BindFactory<PlayerId, MiningFacilityType, Vector3, MiningFacilityEntity, MiningFacilityFactory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<MiningFacilityInstaller>(GetPath<MiningFacilityInstaller>())
                .NonLazy();

            Container
                .BindFactory<PlayerId, MiningFacilityType, Vector3, MiningFacilityEntity, AsteroidMiningFacilityFactory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<AsteroidMiningFacilityInstaller>(GetPath<AsteroidMiningFacilityInstaller>())
                .NonLazy();

            Container
                .BindFactory<PlayerId, DefendPlatformType, Vector3, DefendPlatform, DefendPlatformFactory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<DefendPlatformInstaller>(GetPath<DefendPlatformInstaller>())
                .NonLazy();

            Container
                .BindFactory<PlayerId, DefendPlatformType, Vector3, DefendPlatform, AsteroidDefendPlatformFactory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<AsteroidDefendPlatformInstaller>(GetPath<AsteroidDefendPlatformInstaller>())
                .NonLazy();
        }

        private GameObject GetPath<T>()
        {
            return AssetService.Load<GameObject>(typeof(T).Name);
        }
    }
}
