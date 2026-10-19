using System;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    [Serializable]
    public sealed class UnitTypeSave
    {
        [SerializeField] private bool isShip;
        [SerializeField] private ShipType shipType;
        [SerializeField] private SquadronType squadronType;

        public UnitTypeId UnitType => isShip ? UnitTypeId.Ship(shipType) : UnitTypeId.Squadron(squadronType);

        public static UnitTypeSave From(UnitTypeId unit) => new UnitTypeSave
        {
            isShip = unit.IsShip,
            shipType = unit.IsShip ? unit.ShipType : default,
            squadronType = unit.IsSquadron ? unit.SquadronType : default
        };
    }
}
