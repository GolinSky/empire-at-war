using System;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>One living ship or squadron: identity, pose, health and order. Abilities and hangar are filled in by the save service.</summary>
    [Serializable]
    public sealed class UnitSave
    {
        [SerializeField] private long id;
        [SerializeField] private int owner;
        [SerializeField] private bool isShip;
        [SerializeField] private ShipType shipType;
        [SerializeField] private SquadronType squadronType;
        [SerializeField] private Vector3 position;
        [SerializeField] private Quaternion rotation;
        [SerializeField] private UnitHealthSave health;
        [SerializeField] private UnitOrderSave order;
        [SerializeField] private AbilitySlotSave[] abilities = Array.Empty<AbilitySlotSave>();
        [SerializeField] private bool hasHangar;
        [SerializeField] private HangarSave hangar;

        public long Id => id;
        public PlayerId Owner => new PlayerId(owner);
        public UnitTypeId UnitType => isShip ? UnitTypeId.Ship(shipType) : UnitTypeId.Squadron(squadronType);
        public Vector3 Position => position;
        public Quaternion Rotation => rotation;
        public UnitHealthSave Health => health;
        public UnitOrderSave Order => order;
        public AbilitySlotSave[] Abilities => abilities;
        /// <summary>Null when the unit has no hangar.</summary>
        public HangarSave Hangar => hasHangar ? hangar : null;

        public static UnitSave Create(long id, PlayerId owner, UnitTypeId unitType, Vector3 position, Quaternion rotation,
            UnitHealthSave health, UnitOrderSave order) => new UnitSave
        {
            id = id,
            owner = owner.Index,
            isShip = unitType.IsShip,
            shipType = unitType.IsShip ? unitType.ShipType : default,
            squadronType = unitType.IsSquadron ? unitType.SquadronType : default,
            position = position,
            rotation = rotation,
            health = health,
            order = order
        };

        public UnitSave WithAbilities(AbilitySlotSave[] slots)
        {
            abilities = slots;
            return this;
        }

        public UnitSave WithHangar(HangarSave save)
        {
            hasHangar = true;
            hangar = save;
            return this;
        }
    }
}
