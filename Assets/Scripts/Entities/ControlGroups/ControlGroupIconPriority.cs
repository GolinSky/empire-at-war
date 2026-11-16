using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Entities.ControlGroups
{
    public static class ControlGroupIconPriority
    {
        // Negative means left belongs before right. Enum ordinals are type tie-breakers, not strength.
        public static int Compare(UnitTypeId left, ShipClass leftClass, bool leftHero,
            UnitTypeId right, ShipClass rightClass, bool rightHero)
        {
            int heroOrder = rightHero.CompareTo(leftHero);
            if (heroOrder != 0) return heroOrder;
            int classOrder = Rank(rightClass).CompareTo(Rank(leftClass));
            if (classOrder != 0) return classOrder;
            if (left.IsShip != right.IsShip) return left.IsShip ? -1 : 1;
            return left.IsShip
                ? left.ShipType.CompareTo(right.ShipType)
                : left.SquadronType.CompareTo(right.SquadronType);
        }

        private static int Rank(ShipClass shipClass) => shipClass switch
        {
            ShipClass.HeavyCapital => 8,
            ShipClass.Capital => 7,
            ShipClass.Cruiser => 6,
            ShipClass.Frigate => 5,
            ShipClass.Corvette => 4,
            ShipClass.HeroStrikecraft => 3,
            ShipClass.Bomber => 2,
            ShipClass.Interceptor => 1,
            ShipClass.Fighter => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(shipClass), shipClass, "Not a mobile unit class.")
        };
    }
}
