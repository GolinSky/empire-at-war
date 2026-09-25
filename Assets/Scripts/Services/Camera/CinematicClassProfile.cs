using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    [Serializable]
    public struct CinematicClassProfile
    {
        [SerializeField] private ShipClass shipClass;
        [SerializeField] private float interestWeight;
        [SerializeField] private float framingDistance;

        public CinematicClassProfile(ShipClass shipClass, float interestWeight, float framingDistance)
        {
            this.shipClass = shipClass;
            this.interestWeight = interestWeight;
            this.framingDistance = framingDistance;
        }

        public ShipClass ShipClass => shipClass;
        public float InterestWeight => interestWeight;
        public float FramingDistance => framingDistance;
    }
}
