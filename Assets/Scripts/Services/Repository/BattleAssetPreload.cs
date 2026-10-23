using System.Collections.Generic;
using System.Threading;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Data;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;

namespace EmpireAtWar.Repository
{
    // Loads in three batches; each batch starts every load at once and needs only data from the batches before it.
    public static class BattleAssetPreload
    {
        private static readonly string[] UI_KEYS =
        {
            "UiService", "FadeUi", "FpsUi", "CoreGameUi", "FactionUi", "ShipBuildUi", "ShipUi",
            "ShipGroupUi", "EconomyUi", "MiniMapUi", "ReinforcementUi", "UnitOrderFeedbackUi",
            "TooltipUi", "PauseMenuUi", "SettingsUi", "SaveBrowserUi", "EndGameUi", "HeroUi"
        };
        private static readonly string[] STRUCTURE_PREFIXES = { "", "Asteroid" };
        private static readonly string[] PLATFORMS = { "DefendPlatform", "AotrGolanIIIDefensePlatform", "AotrEmpressDefensePlatform" };

        public static async Awaitable LoadAsync(CachedAssetService assets, BattleDefinition battle,
            CancellationToken cancellationToken)
        {
            BeginShared(assets, battle);
            await assets.CompletePreloadsAsync(cancellationToken);

            FactionCatalog factions = assets.Load<FactionCatalog>(nameof(FactionCatalog));
            ShipsData ships = assets.Load<ShipsData>(nameof(ShipsData));
            SpaceStationData stations = assets.Load<SpaceStationData>(nameof(SpaceStationData));
            HashSet<FactionType> battleFactions = new HashSet<FactionType>();
            foreach (var slot in battle.Slots) battleFactions.Add(slot.Faction);

            BeginStructures(assets);
            foreach (FactionType faction in battleFactions)
            {
                assets.BeginPreload<GameObject>(faction + "SpaceStationView");
                foreach (ShipType ship in factions.Get(faction).Ships.Keys)
                {
                    assets.BeginPreload<ShipData>(ships.GetShipDataPath(ship));
                    assets.BeginPreload<GameObject>(ship + "ShipView");
                }
            }
            await assets.CompletePreloadsAsync(cancellationToken);

            HashSet<SquadronType> squadrons = new HashSet<SquadronType>();
            BeginStructureWrecks(assets);
            foreach (FactionType faction in battleFactions)
            {
                FactionDefinition definition = factions.Get(faction);
                if (stations.TryGetWreck(faction, out UnitWreckData stationWreck)) BeginWreck(assets, stationWreck);
                AddBay(squadrons, stations.GetHangarBay(faction));
                foreach (ShipType ship in definition.Ships.Keys)
                {
                    ShipData data = assets.Load<ShipData>(ships.GetShipDataPath(ship));
                    BeginWreck(assets, data.Wreck);
                    foreach (HangarBay bay in data.HangarBays) AddBay(squadrons, bay);
                }
                foreach (SquadronType squadron in definition.Squadrons.Keys) squadrons.Add(squadron);
            }
            foreach (SquadronType squadron in squadrons)
            {
                assets.BeginPreload<SquadronData>(squadron + "SquadronData");
                assets.BeginPreload<GameObject>(squadron + "SquadronView");
            }
            await assets.CompletePreloadsAsync(cancellationToken);
        }

        private static void BeginShared(CachedAssetService assets, BattleDefinition battle)
        {
            assets.BeginPreload<FactionCatalog>(nameof(FactionCatalog));
            assets.BeginPreload<ShipsData>(nameof(ShipsData));
            assets.BeginPreload<SpaceStationData>(nameof(SpaceStationData));
            assets.BeginPreload<StationLevelData>(nameof(StationLevelData));
            assets.BeginPreload<MiningFacilityCatalog>(nameof(MiningFacilityCatalog));
            assets.BeginPreload<DefendPlatformCatalog>(nameof(DefendPlatformCatalog));
            assets.BeginPreload<SuperWeaponCatalog>(nameof(SuperWeaponCatalog));
            assets.BeginPreload<WeaponsData>(nameof(WeaponsData));
            assets.BeginPreload<DamageMatrixData>(nameof(DamageMatrixData));
            assets.BeginPreload<LayerData>(nameof(LayerData));
            assets.BeginPreload<ReinforcementData>(nameof(ReinforcementData));
            assets.BeginPreload<ShipClassMatchupData>(nameof(ShipClassMatchupData));
            assets.BeginPreload<HardPointOverlayData>(nameof(HardPointOverlayData));
            assets.BeginPreload<TooltipSettings>(nameof(TooltipSettings));
            assets.BeginPreload<TooltipIconData>(nameof(TooltipIconData));
            assets.BeginPreload<MenuData>(nameof(MenuData));
            assets.BeginPreload<ShipUiData>(nameof(ShipUiData));
            assets.BeginPreload<ShipAbilityCatalog>(nameof(ShipAbilityCatalog));
            assets.BeginPreload<MiniMapData>(nameof(MiniMapData));
            assets.BeginPreload<CameraData>(nameof(CameraData));
            assets.BeginPreload<CinematicCameraData>(nameof(CinematicCameraData));
            assets.BeginPreload<SharedSelectionData>(nameof(SharedSelectionData));
            assets.BeginPreload<SuperWeaponData>(nameof(SuperWeaponData));
            assets.BeginPreload<EconomyData>(nameof(EconomyData));
            assets.BeginPreload<CaptureSiteData>(nameof(CaptureSiteData));
            foreach (string key in UI_KEYS) assets.BeginPreload<GameObject>(key);
            if (battle.Mode == GameMode.Survival)
            {
                assets.BeginPreload<SurvivalWaveData>(nameof(SurvivalWaveData));
                assets.BeginPreload<GameObject>("SurvivalHudUi");
            }
            if (battle.Mode == GameMode.BattleSimulation)
                assets.BeginPreload<GameObject>("SimulationHudUi");
        }

        // Asteroid variants exist only for the mining facility and the basic platform.
        private static IEnumerable<string> PlatformKeys(string prefix)
        {
            foreach (string platform in PLATFORMS)
                if (prefix.Length == 0 || platform == "DefendPlatform") yield return prefix + platform;
        }

        private static void BeginStructures(CachedAssetService assets)
        {
            foreach (string prefix in STRUCTURE_PREFIXES)
            {
                assets.BeginPreload<MiningFacilityData>(prefix + nameof(MiningFacilityData));
                assets.BeginPreload<GameObject>(prefix + "MiningFacilityView");
                foreach (string key in PlatformKeys(prefix))
                {
                    assets.BeginPreload<DefendPlatformData>(key + "Data");
                    assets.BeginPreload<GameObject>(key + "View");
                }
            }
        }

        private static void BeginStructureWrecks(CachedAssetService assets)
        {
            foreach (string prefix in STRUCTURE_PREFIXES)
            {
                BeginWreck(assets, assets.Load<MiningFacilityData>(prefix + nameof(MiningFacilityData)).Wreck);
                foreach (string key in PlatformKeys(prefix))
                    BeginWreck(assets, assets.Load<DefendPlatformData>(key + "Data").Wreck);
            }
        }

        private static void BeginWreck(CachedAssetService assets, UnitWreckData wreck)
        {
            if (wreck != null) assets.BeginPreload<GameObject>(wreck.PrefabKey);
        }

        private static void AddBay(HashSet<SquadronType> squadrons, HangarBay bay)
        {
            if (bay.Reserve > 0 && bay.MaxActive > 0) squadrons.Add(bay.SquadronType);
        }
    }
}
