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
    public static class BattleAssetPreload
    {
        private static readonly string[] UI_KEYS =
        {
            "UiService", "FadeUi", "FpsUi", "CoreGameUi", "FactionUi", "ShipBuildUi", "ShipUi",
            "ShipGroupUi", "EconomyUi", "MiniMapUi", "ReinforcementUi", "UnitOrderFeedbackUi",
            "TooltipUi", "PauseMenuUi", "SettingsUi", "SaveBrowserUi", "EndGameUi", "HeroUi"
        };

        public static async Awaitable LoadAsync(CachedAssetService assets, BattleDefinition battle,
            CancellationToken cancellationToken)
        {
            await LoadSharedAsync(assets, cancellationToken);
            foreach (string key in UI_KEYS)
                await assets.PreloadAsync<GameObject>(key, cancellationToken);
            if (battle.Mode == GameMode.Survival)
            {
                await assets.PreloadAsync<SurvivalWaveData>(nameof(SurvivalWaveData), cancellationToken);
                await assets.PreloadAsync<GameObject>("SurvivalHudUi", cancellationToken);
            }
            if (battle.Mode == GameMode.BattleSimulation)
                await assets.PreloadAsync<GameObject>("SimulationHudUi", cancellationToken);

            FactionCatalog factions = assets.Load<FactionCatalog>(nameof(FactionCatalog));
            ShipsData ships = assets.Load<ShipsData>(nameof(ShipsData));
            SpaceStationData stations = assets.Load<SpaceStationData>(nameof(SpaceStationData));
            HashSet<FactionType> loadedFactions = new HashSet<FactionType>();
            HashSet<SquadronType> squadrons = new HashSet<SquadronType>();
            foreach (var slot in battle.Slots)
            {
                if (!loadedFactions.Add(slot.Faction)) continue;
                FactionDefinition faction = factions.Get(slot.Faction);
                await assets.PreloadAsync<GameObject>(slot.Faction + "SpaceStationView", cancellationToken);
                if (stations.TryGetWreck(slot.Faction, out UnitWreckData stationWreck))
                    await LoadWreckAsync(assets, stationWreck, cancellationToken);
                AddBay(squadrons, stations.GetHangarBay(slot.Faction));
                foreach (ShipType ship in faction.Ships.Keys)
                {
                    string dataKey = ships.GetShipDataPath(ship);
                    await assets.PreloadAsync<ShipData>(dataKey, cancellationToken);
                    await assets.PreloadAsync<GameObject>(ship + "ShipView", cancellationToken);
                    ShipData data = assets.Load<ShipData>(dataKey);
                    await LoadWreckAsync(assets, data.Wreck, cancellationToken);
                    foreach (HangarBay bay in data.HangarBays) AddBay(squadrons, bay);
                }
                foreach (SquadronType squadron in faction.Squadrons.Keys) squadrons.Add(squadron);
            }
            foreach (SquadronType squadron in squadrons)
            {
                await assets.PreloadAsync<SquadronData>(squadron + "SquadronData", cancellationToken);
                await assets.PreloadAsync<GameObject>(squadron + "SquadronView", cancellationToken);
            }
        }

        private static async Awaitable LoadSharedAsync(CachedAssetService assets, CancellationToken cancellationToken)
        {
            await assets.PreloadAsync<FactionCatalog>(nameof(FactionCatalog), cancellationToken);
            await assets.PreloadAsync<ShipsData>(nameof(ShipsData), cancellationToken);
            await assets.PreloadAsync<SpaceStationData>(nameof(SpaceStationData), cancellationToken);
            await assets.PreloadAsync<StationLevelData>(nameof(StationLevelData), cancellationToken);
            await assets.PreloadAsync<MiningFacilityCatalog>(nameof(MiningFacilityCatalog), cancellationToken);
            await assets.PreloadAsync<DefendPlatformCatalog>(nameof(DefendPlatformCatalog), cancellationToken);
            await assets.PreloadAsync<SuperWeaponCatalog>(nameof(SuperWeaponCatalog), cancellationToken);
            await assets.PreloadAsync<WeaponsData>(nameof(WeaponsData), cancellationToken);
            await assets.PreloadAsync<DamageMatrixData>(nameof(DamageMatrixData), cancellationToken);
            await assets.PreloadAsync<LayerData>(nameof(LayerData), cancellationToken);
            await assets.PreloadAsync<ReinforcementData>(nameof(ReinforcementData), cancellationToken);
            await assets.PreloadAsync<ShipClassMatchupData>(nameof(ShipClassMatchupData), cancellationToken);
            await assets.PreloadAsync<HardPointOverlayData>(nameof(HardPointOverlayData), cancellationToken);
            await assets.PreloadAsync<TooltipSettings>(nameof(TooltipSettings), cancellationToken);
            await assets.PreloadAsync<TooltipIconData>(nameof(TooltipIconData), cancellationToken);
            await assets.PreloadAsync<MenuData>(nameof(MenuData), cancellationToken);
            await assets.PreloadAsync<ShipUiData>(nameof(ShipUiData), cancellationToken);
            await assets.PreloadAsync<ShipAbilityCatalog>(nameof(ShipAbilityCatalog), cancellationToken);
            await assets.PreloadAsync<MiniMapData>(nameof(MiniMapData), cancellationToken);
            await assets.PreloadAsync<CameraData>(nameof(CameraData), cancellationToken);
            await assets.PreloadAsync<CinematicCameraData>(nameof(CinematicCameraData), cancellationToken);
            await assets.PreloadAsync<SharedSelectionData>(nameof(SharedSelectionData), cancellationToken);
            await assets.PreloadAsync<SuperWeaponData>(nameof(SuperWeaponData), cancellationToken);
            await assets.PreloadAsync<EconomyData>(nameof(EconomyData), cancellationToken);
            await assets.PreloadAsync<CaptureSiteData>(nameof(CaptureSiteData), cancellationToken);
            foreach (string prefix in new[] { "", "Asteroid" })
            {
                await assets.PreloadAsync<MiningFacilityData>(prefix + nameof(MiningFacilityData), cancellationToken);
                await assets.PreloadAsync<GameObject>(prefix + "MiningFacilityView", cancellationToken);
                await LoadWreckAsync(assets, assets.Load<MiningFacilityData>(prefix + nameof(MiningFacilityData)).Wreck,
                    cancellationToken);
                foreach (string platform in new[] { "DefendPlatform", "AotrGolanIIIDefensePlatform", "AotrEmpressDefensePlatform" })
                {
                    if (prefix.Length > 0 && platform != "DefendPlatform") continue;
                    string key = prefix + platform;
                    await assets.PreloadAsync<DefendPlatformData>(key + "Data", cancellationToken);
                    await assets.PreloadAsync<GameObject>(key + "View", cancellationToken);
                    await LoadWreckAsync(assets, assets.Load<DefendPlatformData>(key + "Data").Wreck, cancellationToken);
                }
            }
        }

        private static async Awaitable LoadWreckAsync(CachedAssetService assets, UnitWreckData wreck,
            CancellationToken cancellationToken)
        {
            if (wreck != null) await assets.PreloadAsync<GameObject>(wreck.PrefabKey, cancellationToken);
        }

        private static void AddBay(HashSet<SquadronType> squadrons, HangarBay bay)
        {
            if (bay.Reserve > 0 && bay.MaxActive > 0) squadrons.Add(bay.SquadronType);
        }
    }
}
