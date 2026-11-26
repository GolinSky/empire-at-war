using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Lazily rates every ship, squadron and defend platform type from its live data assets, so balance edits to hull, shields,
    /// weapons or the damage matrix reach the AI without a re-bake.
    /// </summary>
    public sealed class UnitCombatProfileCatalog
    {
        private const string SQUADRON_DATA_POSTFIX = nameof(SquadronData);

        private readonly IAssetService _assetService;
        private readonly ShipsData _shipsData;
        private readonly WeaponsData _weaponsData;
        private readonly DamageMatrixData _damageMatrix;
        private readonly Dictionary<UnitTypeId, UnitCombatProfile> _profiles =
            new Dictionary<UnitTypeId, UnitCombatProfile>();
        private readonly Dictionary<DefendPlatformType, UnitCombatProfile> _platformProfiles =
            new Dictionary<DefendPlatformType, UnitCombatProfile>();
        private FactionCatalog _factions;

        public UnitCombatProfileCatalog(
            IAssetService assetService,
            ShipsData shipsData,
            WeaponsData weaponsData,
            DamageMatrixData damageMatrix)
        {
            _assetService = assetService;
            _shipsData = shipsData;
            _weaponsData = weaponsData;
            _damageMatrix = damageMatrix;
        }

        public UnitCombatProfile Get(UnitTypeId unitTypeId)
        {
            if (_profiles.TryGetValue(unitTypeId, out UnitCombatProfile profile))
            {
                return profile;
            }

            profile = unitTypeId.IsShip
                ? CreateShip(unitTypeId.ShipType)
                : CreateSquadron(unitTypeId.SquadronType);
            _profiles.Add(unitTypeId, profile);
            return profile;
        }

        private UnitCombatProfile CreateShip(ShipType shipType)
        {
            ShipData data = _assetService.Load<ShipData>(_shipsData.GetShipDataPath(shipType));
            List<UnitCombatProfile> hangar = new List<UnitCombatProfile>();
            foreach (HangarBay bay in data.HangarBays)
            {
                UnitCombatProfile squadron = Get(UnitTypeId.Squadron(bay.SquadronType));
                for (int i = 0; i < bay.MaxActive; i++)
                {
                    hangar.Add(squadron);
                }
            }

            if (data.HangarMaxActive >= 0 && hangar.Count > data.HangarMaxActive)
                hangar.RemoveRange(data.HangarMaxActive, hangar.Count - data.HangarMaxActive);

            return UnitCombatProfileFactory.Create(data.ShipClass, data.Hull, data.Shields,
                data.WeaponLoadout, _weaponsData, _damageMatrix, hangar,
                shieldRegen: data.ShieldRegenerateValue / data.ShieldRegenerateDelay, speed: data.Speed,
                price: GetShipPrice(shipType), faction: GetShipFaction(shipType), roles: data.Roles);
        }

        /// <summary>
        /// A structure rated from the weapons it fires now (a station's live hardpoints): no stored loadout exists for
        /// stations, and their weapons change with the station level and with destroyed hardpoints.
        /// </summary>
        public UnitCombatProfile CreateStructure(ShipClass shipClass, float hull, float shields,
            IReadOnlyList<WeaponLoadoutEntry> loadout) =>
            UnitCombatProfileFactory.Create(shipClass, hull, shields, loadout, _weaponsData, _damageMatrix,
                Array.Empty<UnitCombatProfile>());

        // Ships and squadrons are sold by the faction that lists them; a type nobody sells has no price.
        private float GetShipPrice(ShipType shipType)
        {
            foreach (FactionDefinition faction in GetFactions().Factions)
                if (faction.Ships.TryGetValue(shipType, out FactionData data)) return data.Price;
            return 0f;
        }

        // The faction that lists a unit is the unit's own faction, which owns its faction damage scales.
        private FactionType? GetShipFaction(ShipType shipType)
        {
            foreach (FactionDefinition faction in GetFactions().Factions)
                if (faction.Ships.ContainsKey(shipType)) return faction.FactionType;
            return null;
        }

        private FactionType? GetSquadronFaction(SquadronType squadronType)
        {
            foreach (FactionDefinition faction in GetFactions().Factions)
                if (faction.Squadrons.ContainsKey(squadronType)) return faction.FactionType;
            return null;
        }

        private float GetSquadronPrice(SquadronType squadronType)
        {
            foreach (FactionDefinition faction in GetFactions().Factions)
                if (faction.Squadrons.TryGetValue(squadronType, out FactionData data)) return data.Price;
            return 0f;
        }

        private FactionCatalog GetFactions() => _factions ??= _assetService.Load<FactionCatalog>(nameof(FactionCatalog));

        /// <summary>A satellite or platform rated from its weapon loadout: turbolasers hurt wings little, quad lasers a lot.</summary>
        public UnitCombatProfile GetPlatform(DefendPlatformType type)
        {
            if (_platformProfiles.TryGetValue(type, out UnitCombatProfile profile)) return profile;
            DefendPlatformData data = _assetService.Load<DefendPlatformData>(DefendPlatformAssets.GetDataName(type));
            profile = UnitCombatProfileFactory.Create(data.ComponentData.ShipClass, data.ComponentData.Hull,
                data.ComponentData.Shields, data.WeaponLoadout, _weaponsData, _damageMatrix,
                Array.Empty<UnitCombatProfile>(),
                shieldRegen: data.ComponentData.ShieldRegenerateValue / data.ComponentData.ShieldRegenerateDelay);
            _platformProfiles.Add(type, profile);
            return profile;
        }

        public float GetShipTravelTime(ShipType shipType, float distance)
        {
            ShipData data = _assetService.Load<ShipData>(_shipsData.GetShipDataPath(shipType));
            return distance / data.Speed;
        }

        public float GetSquadronTravelTime(SquadronType squadronType, float distance)
        {
            SquadronData data = _assetService.Load<SquadronData>(squadronType + SQUADRON_DATA_POSTFIX);
            return distance / data.CruiseSpeed;
        }

        private UnitCombatProfile CreateSquadron(SquadronType squadronType)
        {
            SquadronData data = _assetService.Load<SquadronData>(squadronType + SQUADRON_DATA_POSTFIX);
            return UnitCombatProfileFactory.Create(data.ShipClass,
                data.TotalHull,
                data.TotalShields,
                data.WeaponLoadout, _weaponsData, _damageMatrix,
                Array.Empty<UnitCombatProfile>(), data.WarheadDamageMultiplier, data.WarheadReloadMultiplier,
                data.ShieldRegenerateValue * data.MemberCount / data.ShieldRegenerateDelay, data.CruiseSpeed,
                GetSquadronPrice(squadronType), GetSquadronFaction(squadronType));
        }
    }
}
