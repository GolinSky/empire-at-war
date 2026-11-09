using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Entities.ShipUi;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Hazards;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.Vision;
using VContainer;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Builds hint conditions. Systems that only exist in some modes (economy, capture sites, Survival) are looked up
    /// once; a hint about a missing system gets a condition that never fires, because that situation cannot happen.
    /// </summary>
    public sealed class HintConditionFactory
    {
        private readonly ISelectionService _selection;
        private readonly IPlayerFactionModelObserver _faction;
        private readonly IFactionResearchModelObserver _research;
        private readonly ICameraInput _camera;
        private readonly IUnitOrderService _orders;
        private readonly ILocalPlayer _player;
        private readonly IMiniMapInputObserver _miniMap;
        private readonly ITooltipModelObserver _tooltip;
        private readonly ShipAbilityService _abilities;
        private readonly IAutoPowerToggleObserver _autoPower;
        private readonly IEntityLocator _entities;
        private readonly IVisionService _vision;
        private readonly FactionCatalog _factions;
        private readonly IReinforcementModelObserver _reinforcements;
        private readonly ISuperWeaponModelObserver _superWeapons;
        private readonly IHazardService _hazards;
        private readonly IEconomyModelObserver _economy;
        private readonly ICaptureSiteDirectory _sites;
        private readonly SurvivalModel _survival;

        public HintConditionFactory(ISelectionService selection, IPlayerFactionModelObserver faction,
            IFactionResearchModelObserver research, ICameraInput camera, IUnitOrderService orders, ILocalPlayer player,
            IMiniMapInputObserver miniMap, ITooltipModelObserver tooltip, ShipAbilityService abilities,
            IAutoPowerToggleObserver autoPower, IEntityLocator entities, IVisionService vision, FactionCatalog factions,
            IReinforcementModelObserver reinforcements, ISuperWeaponModelObserver superWeapons, IHazardService hazards,
            IObjectResolver resolver)
        {
            _selection = selection;
            _faction = faction;
            _research = research;
            _camera = camera;
            _orders = orders;
            _player = player;
            _miniMap = miniMap;
            _tooltip = tooltip;
            _abilities = abilities;
            _autoPower = autoPower;
            _entities = entities;
            _vision = vision;
            _factions = factions;
            _reinforcements = reinforcements;
            _superWeapons = superWeapons;
            _hazards = hazards;
            _economy = ResolveOptional<IEconomyModelObserver>(resolver);
            _sites = ResolveOptional<ICaptureSiteDirectory>(resolver);
            _survival = ResolveOptional<SurvivalModel>(resolver);
        }

        public IHintCondition CreateTrigger(HintConditionId id) =>
            id == HintConditionId.None ? new ImmediateHintCondition() : Create(id);

        public IHintCondition CreateCompletion(HintConditionId id) =>
            id == HintConditionId.None ? new NeverHintCondition() : Create(id);

        private IHintCondition Create(HintConditionId id)
        {
            switch (id)
            {
                case HintConditionId.ShipSelected: return new SelectionHintCondition(_selection, SelectionType.Ship, 1);
                case HintConditionId.MultipleShipsSelected:
                    return new SelectionHintCondition(_selection, SelectionType.Ship, 2);
                case HintConditionId.StationSelected:
                    return new SelectionHintCondition(_selection, SelectionType.Base, 1);
                case HintConditionId.LevelUpQueued:
                    return new QueuedRequestHintCondition(_faction, typeof(LevelUnitRequest));
                case HintConditionId.ResearchQueued:
                    return new QueuedRequestHintCondition(_faction, typeof(ResearchUnitRequest));
                case HintConditionId.MiningSiteCaptured: return Site(CaptureSiteKind.Mining, CaptureSiteStage.Captured);
                case HintConditionId.MiningSiteBuilding: return Site(CaptureSiteKind.Mining, CaptureSiteStage.Building);
                case HintConditionId.UnitQueued: return new UnitQueuedHintCondition(_faction, 1);
                case HintConditionId.SeveralUnitsQueued: return new UnitQueuedHintCondition(_faction, 2);
                case HintConditionId.UnitCompleted: return new UnitCompletedHintCondition(_faction);
                case HintConditionId.LevelUpgraded: return new LevelUpgradedHintCondition(_faction);
                case HintConditionId.ResearchCompleted: return new ResearchCompletedHintCondition(_research);
                case HintConditionId.CameraPanned: return new CameraPannedHintCondition(_camera);
                case HintConditionId.CameraZoomed: return new CameraZoomedHintCondition(_camera);
                case HintConditionId.MoveOrdered: return Order(UnitActionId.Move);
                case HintConditionId.AttackOrdered: return Order(UnitActionId.Attack);
                case HintConditionId.AttackMoveOrdered: return Order(UnitActionId.AttackMove);
                case HintConditionId.WaypointMoveOrdered: return Order(UnitActionId.WaypointMove);
                case HintConditionId.StopOrdered: return Order(UnitActionId.Stop);
                case HintConditionId.GuardOrdered: return Order(UnitActionId.Guard);
                case HintConditionId.MiniMapCameraMoved: return new MiniMapInputHintCondition(_miniMap, false);
                case HintConditionId.MiniMapMoveOrdered: return new MiniMapInputHintCondition(_miniMap, true);
                case HintConditionId.MatchupTooltipViewed: return new MatchupTooltipHintCondition(_tooltip);
                case HintConditionId.ShipWithAbilitiesSelected:
                    return new ShipWithAbilitiesSelectedHintCondition(_selection, _player);
                case HintConditionId.AbilityUsed: return new AbilityUsedHintCondition(_abilities, _player);
                case HintConditionId.AutoPowerToggled: return new AutoPowerToggledHintCondition(_autoPower);
                case HintConditionId.LevelUpAffordable:
                    return _economy == null
                        ? new NeverHintCondition()
                        : new LevelUpAffordableHintCondition(_faction, _economy);
                case HintConditionId.ResearchAffordable:
                    return _economy == null
                        ? new NeverHintCondition()
                        : new ResearchAffordableHintCondition(_research, _faction, _economy);
                case HintConditionId.EnemySeen: return new EnemySeenHintCondition(_entities, _player, _vision);
                case HintConditionId.MiningSiteSeen: return Site(CaptureSiteKind.Mining, CaptureSiteStage.Revealed);
                case HintConditionId.MiningSiteOwned:
                    return Site(CaptureSiteKind.Mining, CaptureSiteStage.MiningOperational);
                case HintConditionId.SatelliteSiteSeen: return Site(CaptureSiteKind.Satellite, CaptureSiteStage.Revealed);
                case HintConditionId.ReinforcementAvailable:
                    return new ReinforcementAvailableHintCondition(_reinforcements);
                case HintConditionId.SuperWeaponReady: return new SuperWeaponReadyHintCondition(_superWeapons);
                case HintConditionId.HeroAvailable: return LocalUnit(LocalUnitKind.Hero);
                case HintConditionId.SquadronAvailable:
                case HintConditionId.CarrierOwned: return LocalUnit(LocalUnitKind.Carrier);
                case HintConditionId.ShipWithAbilitiesOwned: return LocalUnit(LocalUnitKind.AbilityShip);
                case HintConditionId.SquadronOrdered: return new SquadronOrderedHintCondition(_orders, _selection, _player);
                case HintConditionId.ReinforcementReady: return new ReinforcementReadyHintCondition(_reinforcements);
                case HintConditionId.ReinforcementDeployed: return new ReinforcementDeployedHintCondition(_reinforcements);
                case HintConditionId.MoveIntoFog: return new MoveIntoFogHintCondition(_orders, _player, _vision);
                case HintConditionId.SatelliteSiteBuilding: return Site(CaptureSiteKind.Satellite, CaptureSiteStage.Building);
                case HintConditionId.HazardEntered: return new HazardEnteredHintCondition(_hazards, _player, _factions);
                case HintConditionId.FogActive: return new FogActiveHintCondition(_vision);
                case HintConditionId.SurvivalPreparation:
                    return _survival == null
                        ? new NeverHintCondition()
                        : new SurvivalPreparationHintCondition(_survival);
                default: throw new ArgumentOutOfRangeException(nameof(id), id, "No hint condition for this id.");
            }
        }

        private IHintCondition LocalUnit(LocalUnitKind kind) => new LocalUnitHintCondition(_entities, _player, _factions, kind);

        private IHintCondition Order(UnitActionId action) => new OrderIssuedHintCondition(_orders, _player, action);

        private IHintCondition Site(CaptureSiteKind kind, CaptureSiteStage stage) => _sites == null
            ? new NeverHintCondition()
            : new CaptureSiteHintCondition(_sites, _player, kind, stage);

        private static T ResolveOptional<T>(IObjectResolver resolver) where T : class =>
            resolver.TryResolve(typeof(T), out object instance) ? (T)instance : null;
    }
}
