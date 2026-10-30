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
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;

namespace EmpireAtWar.Repository
{
    // Loads what the battle needs before Running in three batches; each batch starts every load at once and needs
    // only data from the batches before it. Unit views and wrecks are first used after Running, so they are left
    // to BattleAssetWarmup unless a restore or simulation spawns units during startup.
    public static class BattleAssetPreload
    {
        private static readonly string[] UI_KEYS =
        {
            "UiService", "FadeUi", "FpsUi", "CoreGameUi", "FactionUi", "ShipBuildUi", "ShipUi",
            "ShipGroupUi", "EconomyUi", "MiniMapUi", "ReinforcementUi", "UnitOrderFeedbackUi",
            "TooltipUi", "PauseMenuUi", "SettingsUi", "SaveBrowserUi", "EndGameUi", "HeroUi"
        };
        private static readonly string[] STRUCTURE_PREFIXES = { "", "Asteroid" };
        private static readonly MiningFacilityType[] MINING_FACILITIES =
            { MiningFacilityType.CommonMiner, MiningFacilityType.AsteroidMiner, MiningFacilityType.TibannaGasStation };
        private static readonly string[] PLATFORMS = { "DefendPlatform", "AotrGolanIIIDefensePlatform", "AotrEmpressDefensePlatform" };

        public static async Awaitable LoadAsync(CachedAssetService assets, BattleDefinition battle, bool restoresSave,
            CancellationToken cancellationToken)
        {
            BeginShared(assets, battle);
            await assets.CompletePreloadsAsync(cancellationToken);

            FactionCatalog factions = assets.Load<FactionCatalog>(nameof(FactionCatalog));
            ShipsData ships = assets.Load<ShipsData>(nameof(ShipsData));
            BeginStructureData(assets);
            foreach (FactionType faction in Factions(battle))
            {
                assets.BeginPreload<GameObject>(faction + "SpaceStationView");
                foreach (ShipType ship in factions.Get(faction).Ships.Keys)
                    assets.BeginPreload<ShipData>(ships.GetShipDataPath(ship));
            }
            foreach (string key in PreviewKeys(assets, battle, factions)) assets.BeginPreload<GameObject>(key);
            await assets.CompletePreloadsAsync(cancellationToken);

            foreach (SquadronType squadron in Squadrons(assets, battle))
            {
                assets.BeginPreload<SquadronData>(squadron + "SquadronData");
                assets.BeginPreload<GameObject>(squadron + "SquadronView");
            }
            if (restoresSave || battle.Mode == GameMode.BattleSimulation)
            {
                foreach (string key in DeferredPrefabKeys(assets, battle)) assets.BeginPreload<GameObject>(key);
            }
            await assets.CompletePreloadsAsync(cancellationToken);
        }

        // Prefabs first instantiated after startup: ship and structure views, then every wreck. Needs the data
        // loaded by LoadAsync.
        public static List<string> DeferredPrefabKeys(CachedAssetService assets, BattleDefinition battle)
        {
            FactionCatalog factions = assets.Load<FactionCatalog>(nameof(FactionCatalog));
            ShipsData ships = assets.Load<ShipsData>(nameof(ShipsData));
            SpaceStationData stations = assets.Load<SpaceStationData>(nameof(SpaceStationData));
            List<string> views = new List<string>();
            List<string> wrecks = new List<string>();
            foreach (FactionType faction in Factions(battle))
            {
                foreach (ShipType ship in factions.Get(faction).Ships.Keys)
                {
                    views.Add(ship + "ShipView");
                    AddWreck(wrecks, assets.Load<ShipData>(ships.GetShipDataPath(ship)).Wreck);
                }
                if (stations.TryGetWreck(faction, out UnitWreckData stationWreck)) AddWreck(wrecks, stationWreck);
            }
            foreach (MiningFacilityType type in MINING_FACILITIES)
            {
                string assetName = MiningFacilityAssets.GetName(type);
                // The basic facility view is required while the battle scope builds.
                if (type != MiningFacilityType.CommonMiner) views.Add(assetName + "View");
                AddWreck(wrecks, assets.Load<MiningFacilityData>(assetName + "Data").Wreck);
            }
            foreach (string prefix in STRUCTURE_PREFIXES)
            {
                foreach (string key in PlatformKeys(prefix))
                {
                    if (key != "DefendPlatform") views.Add(key + "View");
                    AddWreck(wrecks, assets.Load<DefendPlatformData>(key + "Data").Wreck);
                }
            }
            views.AddRange(wrecks);
            return views;
        }

        private static HashSet<FactionType> Factions(BattleDefinition battle)
        {
            HashSet<FactionType> factions = new HashSet<FactionType>();
            foreach (var slot in battle.Slots) factions.Add(slot.Faction);
            return factions;
        }

        // Reinforcement placement previews for the factions whose units the local player places: their own and any
        // hosted ally's. Other factions never show a preview, so their art stays unloaded.
        private static HashSet<string> PreviewKeys(CachedAssetService assets, BattleDefinition battle, FactionCatalog factions)
        {
            ReinforcementData reinforcement = assets.Load<ReinforcementData>(nameof(ReinforcementData));
            HashSet<string> keys = new HashSet<string>();
            foreach (BattlePlayer player in battle.Players)
            {
                if (player.Role != PlayerRole.Human && !player.IsHosted) continue;
                keys.UnionWith(reinforcement.GetPreviewKeys(factions.Get(player.Slot.Faction)));
            }
            return keys;
        }

        // Squadrons the factions can build plus every type a ship or station hangar launches; SquadronPool
        // prewarms them during startup.
        internal static HashSet<SquadronType> Squadrons(IAssetService assets, BattleDefinition battle)
        {
            FactionCatalog factions = assets.Load<FactionCatalog>(nameof(FactionCatalog));
            ShipsData ships = assets.Load<ShipsData>(nameof(ShipsData));
            SpaceStationData stations = assets.Load<SpaceStationData>(nameof(SpaceStationData));
            HashSet<SquadronType> squadrons = new HashSet<SquadronType>();
            foreach (FactionType faction in Factions(battle))
            {
                FactionDefinition definition = factions.Get(faction);
                AddBay(squadrons, stations.GetHangarBay(faction));
                foreach (ShipType ship in definition.Ships.Keys)
                {
                    foreach (HangarBay bay in assets.Load<ShipData>(ships.GetShipDataPath(ship)).HangarBays)
                        AddBay(squadrons, bay);
                }
                foreach (SquadronType squadron in definition.Squadrons.Keys) squadrons.Add(squadron);
            }
            return squadrons;
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

        internal static IEnumerable<string> PlatformDataKeys()
        {
            foreach (string prefix in STRUCTURE_PREFIXES)
                foreach (string key in PlatformKeys(prefix)) yield return key + "Data";
        }

        // SkirmishScope reads the basic platform and facility colliders while it builds.
        private static void BeginStructureData(CachedAssetService assets)
        {
            assets.BeginPreload<GameObject>("DefendPlatformView");
            assets.BeginPreload<GameObject>("MiningFacilityView");
            foreach (MiningFacilityType type in MINING_FACILITIES)
                assets.BeginPreload<MiningFacilityData>(MiningFacilityAssets.GetName(type) + "Data");
            foreach (string key in PlatformDataKeys()) assets.BeginPreload<DefendPlatformData>(key);
        }

        private static void AddWreck(List<string> keys, UnitWreckData wreck)
        {
            if (wreck != null) keys.Add(wreck.PrefabKey);
        }

        private static void AddBay(HashSet<SquadronType> squadrons, HangarBay bay)
        {
            if (bay.Reserve > 0 && bay.MaxActive > 0) squadrons.Add(bay.SquadronType);
        }
    }
}
