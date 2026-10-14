using System;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    [Serializable]
    public sealed class SurvivalWaveEntry
    {
        [SerializeField] private bool isSquadron;
        [SerializeField] private int unitId;
        [SerializeField, Min(1)] private int quantity;
        public UnitTypeId Unit => isSquadron ? UnitTypeId.Squadron((SquadronType)unitId) : UnitTypeId.Ship((ShipType)unitId);
        public int Quantity => quantity;

        public SurvivalWaveEntry(UnitTypeId unit, int count)
        {
            isSquadron = unit.IsSquadron;
            unitId = unit.IsShip ? (int)unit.ShipType : (int)unit.SquadronType;
            quantity = count;
        }
    }
}
