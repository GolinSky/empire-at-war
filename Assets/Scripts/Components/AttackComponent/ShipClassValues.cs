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
        [SerializeField] private float interceptor;
        [SerializeField] private float corvette;
        [SerializeField] private float frigate;
        [SerializeField] private float cruiser;
        [SerializeField] private float capital;
        [SerializeField] private float heavyCapital;
        [SerializeField] private float structure;
        [SerializeField] private float heroStrikecraft;

        public float this[ShipClass shipClass] => shipClass switch
        {
            ShipClass.Fighter => fighter,
            ShipClass.Bomber => bomber,
            ShipClass.Interceptor => interceptor,
            ShipClass.Corvette => corvette,
            ShipClass.Frigate => frigate,
            ShipClass.Cruiser => cruiser,
            ShipClass.Capital => capital,
            ShipClass.HeavyCapital => heavyCapital,
            ShipClass.Structure => structure,
            ShipClass.HeroStrikecraft => heroStrikecraft,
            _ => throw new ArgumentOutOfRangeException(nameof(shipClass), shipClass, null)
        };

        public ShipClassValues(float fighter, float bomber, float interceptor, float corvette, float frigate, float cruiser,
            float capital, float heavyCapital, float structure, float heroStrikecraft)
        {
            this.fighter = fighter;
            this.bomber = bomber;
            this.interceptor = interceptor;
            this.corvette = corvette;
            this.frigate = frigate;
            this.cruiser = cruiser;
            this.capital = capital;
            this.heavyCapital = heavyCapital;
            this.structure = structure;
            this.heroStrikecraft = heroStrikecraft;
        }
    }
}
