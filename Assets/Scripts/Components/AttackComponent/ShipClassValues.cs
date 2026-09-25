using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    [Serializable]
    public struct ShipClassValues
    {
        [SerializeField] private float fighter;
        [SerializeField] private float bomber;
        [SerializeField] private float corvette;
        [SerializeField] private float frigate;
        [SerializeField] private float capital;
        [SerializeField] private float heavyCapital;
        [SerializeField] private float structure;

        public ShipClassValues(float fighter, float bomber, float corvette, float frigate, float capital,
            float heavyCapital, float structure)
        {
            this.fighter = fighter;
            this.bomber = bomber;
            this.corvette = corvette;
            this.frigate = frigate;
            this.capital = capital;
            this.heavyCapital = heavyCapital;
            this.structure = structure;
        }

        public float this[ShipClass shipClass] => shipClass switch
        {
            ShipClass.Fighter => fighter,
            ShipClass.Bomber => bomber,
            ShipClass.Corvette => corvette,
            ShipClass.Frigate => frigate,
            ShipClass.Capital => capital,
            ShipClass.HeavyCapital => heavyCapital,
            ShipClass.Structure => structure,
            _ => throw new ArgumentOutOfRangeException(nameof(shipClass), shipClass, null)
        };
    }
}
