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
                shieldRegen: data.ShieldRegenerateValue / data.ShieldRegenerateDelay);
        }

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
                data.ShieldRegenerateValue * data.MemberCount / data.ShieldRegenerateDelay);
        }
    }
}
