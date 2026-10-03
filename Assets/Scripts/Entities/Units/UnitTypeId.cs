using System;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.Units
{
    /// <summary>Identifies a selectable unit kind: one ship type or one squadron type.</summary>
    public readonly struct UnitTypeId : IEquatable<UnitTypeId>
    {
        private readonly Category _category;

        private readonly int _type;

        public bool IsShip => _category == Category.Ship;
        public bool IsSquadron => _category == Category.Squadron;

        public ShipType ShipType => IsShip
            ? (ShipType)_type
            : throw new InvalidOperationException($"{this} is not a ship type.");

        public SquadronType SquadronType => IsSquadron
            ? (SquadronType)_type
            : throw new InvalidOperationException($"{this} is not a squadron type.");

        private UnitTypeId(Category category, int type)
        {
            _category = category;
            _type = type;
        }

        public static UnitTypeId Ship(ShipType shipType) => new UnitTypeId(Category.Ship, (int)shipType);

        public static UnitTypeId Squadron(SquadronType squadronType) => new UnitTypeId(Category.Squadron, (int)squadronType);

        public bool Equals(UnitTypeId other) => _category == other._category && _type == other._type;

        public override bool Equals(object obj) => obj is UnitTypeId other && Equals(other);

        public override int GetHashCode() => ((int)_category * 397) ^ _type;

        public override string ToString() => IsShip ? ((ShipType)_type).ToString() : ((SquadronType)_type).ToString();

        public static bool operator ==(UnitTypeId left, UnitTypeId right) => left.Equals(right);

        public static bool operator !=(UnitTypeId left, UnitTypeId right) => !left.Equals(right);

        private enum Category
        {
            Ship,
            Squadron
        }
    }
}
