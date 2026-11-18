using System;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    /// <summary>A group of destroyed entities of one type, or of one station faction and level.</summary>
    [Serializable]
    public sealed class BattleUnitLoss
    {
        [SerializeField] private BattleLossKind kind;
        [SerializeField] private int typeId;
        [SerializeField] private int count;
        [SerializeField] private int stationLevel;

        public BattleLossKind Kind => kind;
        internal int TypeId => typeId;
        public int Count => count;
        public int StationLevel => stationLevel;
        public FactionType StationFaction => (FactionType)typeId;
        public DefendPlatformType DefendPlatformType => (DefendPlatformType)typeId;
        public MiningFacilityType MiningFacilityType => (MiningFacilityType)typeId;
        public UnitTypeId UnitType => Kind switch
        {
            BattleLossKind.Ship => UnitTypeId.Ship((ShipType)typeId),
            BattleLossKind.Squadron => UnitTypeId.Squadron((SquadronType)typeId),
            _ => throw new InvalidOperationException($"A {Kind} loss does not have a ship or squadron type.")
        };

        public BattleUnitLoss(BattleLossKind kind, int typeId, int count, int stationLevel = 0)
        {
            this.kind = kind;
            this.typeId = typeId;
            this.count = count;
            this.stationLevel = stationLevel;
        }
    }
}
