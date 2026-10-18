using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Data;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Mvc;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>Read-only battle catalogs shared by every player and entity scope.</summary>
    public static class BattleDataRegistrations
    {
        public static void Register(IContainerBuilder builder, IAssetService assets)
        {
            AssetRegistrations.RegisterShared<StationLevelData>(builder, assets);
            AssetRegistrations.RegisterShared<MiningFacilityCatalog>(builder, assets);
            AssetRegistrations.RegisterShared<DefendPlatformCatalog>(builder, assets);
            AssetRegistrations.RegisterShared<SuperWeaponCatalog>(builder, assets);
            AssetRegistrations.RegisterShared<WeaponsData>(builder, assets);
            AssetRegistrations.RegisterShared<DamageMatrixData>(builder, assets);
            AssetRegistrations.RegisterShared<LayerData>(builder, assets);
            AssetRegistrations.RegisterShared<ShipsData>(builder, assets);
            AssetRegistrations.RegisterShared<ReinforcementData>(builder, assets);
            AssetRegistrations.RegisterShared<ShipClassMatchupData>(builder, assets);
        }
    }
}
