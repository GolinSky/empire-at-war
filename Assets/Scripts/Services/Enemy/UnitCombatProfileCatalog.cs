using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Hangar;
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
    /// Lazily rates every ship and squadron type from its live data assets, so balance edits to hull, shields,
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

            return UnitCombatProfileFactory.Create(data.ShipClass, data.Hull, data.Shields,
                data.WeaponLoadout, _weaponsData, _damageMatrix, hangar);
        }

        private UnitCombatProfile CreateSquadron(SquadronType squadronType)
        {
            SquadronData data = _assetService.Load<SquadronData>(squadronType + SQUADRON_DATA_POSTFIX);
            return UnitCombatProfileFactory.Create(data.ShipClass,
                data.MemberHull * data.MemberCount,
                data.MemberShields * data.MemberCount,
                data.WeaponLoadout, _weaponsData, _damageMatrix,
                Array.Empty<UnitCombatProfile>());
        }
    }
}
