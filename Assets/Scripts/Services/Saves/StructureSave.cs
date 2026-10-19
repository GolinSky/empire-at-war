using System;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>One living station, mining facility or defense platform; its level follows the owner's station level.</summary>
    [Serializable]
    public sealed class StructureSave
    {
        [SerializeField] private long id;
        [SerializeField] private int owner;
        [SerializeField] private StructureKind kind;
        [SerializeField] private int type;
        [SerializeField] private Vector3 position;
        [SerializeField] private UnitHealthSave health;
        [SerializeField] private bool hasHangar;
        [SerializeField] private HangarSave hangar;

        public long Id => id;
        public PlayerId Owner => new PlayerId(owner);
        public StructureKind Kind => kind;
        public FactionType StationFaction => (FactionType)type;
        public MiningFacilityType MiningFacilityType => (MiningFacilityType)type;
        public DefendPlatformType DefendPlatformType => (DefendPlatformType)type;
        public Vector3 Position => position;
        public UnitHealthSave Health => health;
        /// <summary>Null when the structure has no hangar.</summary>
        public HangarSave Hangar => hasHangar ? hangar : null;

        public static StructureSave Create(long id, PlayerId owner, StructureKind kind, int type, Vector3 position,
            UnitHealthSave health) => new StructureSave
        {
            id = id, owner = owner.Index, kind = kind, type = type, position = position, health = health
        };

        public StructureSave WithHangar(HangarSave save)
        {
            hasHangar = true;
            hangar = save;
            return this;
        }
    }
}
