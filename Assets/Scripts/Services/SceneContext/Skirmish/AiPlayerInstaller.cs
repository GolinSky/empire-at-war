using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.SceneContext.Skirmish;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    /// <summary>
    /// Installs one AI player. The skirmish installer runs it once per AI slot, each in its own
    /// sub-container, after binding that slot's <see cref="PlayerSlot"/>.
    /// </summary>
    public class AiPlayerInstaller : Installer<AiPlayerInstaller>
    {
        [Inject] private IAssetService Repository { get; }
        [Inject] private PlayerSlot Owner { get; }

        public override void InstallBindings()
        {
            Container.Install<GameUnitsInstaller>();

            Container.BindInterfacesExt<EnemyService>();
            Container.Bind<EnemyStrategicDecisionModel>().AsSingle();
            Container.Bind<EnemyProductionDecisionModel>().AsSingle();
            Container.Bind<EnemyStrategicContextBuilder>().AsSingle();
            Container.Bind<EnemyTaskForceExecutor>().AsSingle();
            Container.Bind<EnemyProductionStrategy>().AsSingle();
            Container.Bind<IEnemyStructurePlacementService>().To<EnemyStructurePlacementService>().AsSingle();
            Container.BindInterfacesExt<EnemyUnitCommander>();
            Container.BindInterfacesExt<EnemyShipAbilityController>();
            Container.Bind<SuperWeaponModel>().AsSingle();
            Container.BindInterfacesExt<EnemySuperWeaponController>();
            Container.Bind<EnemyUnitLimitModel>().AsSingle();
            Container.BindScriptableObject<ReinforcementData>(Repository);

            Container.BindInterfacesExt<EnemyPurchaseProcessor>();

            // Registers itself as this AI's pending-reinforcement source in the scene-wide player registry.
            Container.BindInterfacesExt<EnemyFactionController>();

            Container.BindScriptableObject<EconomyData>(Repository);
            Container.BindInterfacesAndSelfTo<EconomyModel>().AsSingle();
            Container.BindInterfacesNonLazyExt<EconomyService>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.Bind<EnemyEconomyDebugView>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
#endif

            // Registers itself in the scene-wide player registry so capture sites can build for this AI.
            Container.BindInterfacesTo<SiteFacilityBuilder>().AsSingle().NonLazy();
            Container.BindInterfacesExt<EnemySiteConstructionController>();
            Container.BindInterfacesExt<EnemySquadronCommander>();

            Container
                .Bind<EnemyFactionModel>()
                .AsSingle()
                .WithArguments(Owner.Faction);
            Container
                .BindInterfacesAndSelfTo<FactionResearchModel>()
                .AsSingle()
                .WithArguments(Owner.Faction);
        }
    }
}
