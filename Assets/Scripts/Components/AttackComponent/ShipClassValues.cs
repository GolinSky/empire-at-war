using System;
using UnityEngine;
using UnityEngine.Serialization;
using EmpireAtWar.Entities.Units;

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
        [SerializeField] private float heavyCruiser;
        [SerializeField] private float capital;
        [FormerlySerializedAs("heavyCapital"), SerializeField] private float battleCruiser;
        [SerializeField] private float dreadnought;
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
            ShipClass.HeavyCruiser => heavyCruiser,
            ShipClass.Capital => capital,
            ShipClass.BattleCruiser => battleCruiser,
            ShipClass.Dreadnought => dreadnought,
            ShipClass.Structure => structure,
            ShipClass.HeroStrikecraft => heroStrikecraft,
            _ => throw new ArgumentOutOfRangeException(nameof(shipClass), shipClass, null)
        };

        public ShipClassValues(float fighter, float bomber, float interceptor, float corvette, float frigate, float cruiser,
            float heavyCruiser, float capital, float battleCruiser, float dreadnought, float structure, float heroStrikecraft)
        {
            this.fighter = fighter;
            this.bomber = bomber;
            this.interceptor = interceptor;
            this.corvette = corvette;
            this.frigate = frigate;
            this.cruiser = cruiser;
            this.heavyCruiser = heavyCruiser;
            this.capital = capital;
            this.battleCruiser = battleCruiser;
            this.dreadnought = dreadnought;
            this.structure = structure;
            this.heroStrikecraft = heroStrikecraft;
        }
    }
}
