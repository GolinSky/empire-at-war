using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    [Serializable]
    public struct CinematicClassProfile
    {
        [field: SerializeField] public ShipClass ShipClass { get; private set; }
        [field: SerializeField] public float InterestWeight { get; private set; }
        [field: SerializeField] public float FramingDistance { get; private set; }

        public CinematicClassProfile(ShipClass shipClass, float interestWeight, float framingDistance)
        {
            ShipClass = shipClass;
            InterestWeight = interestWeight;
            FramingDistance = framingDistance;
        }
    }
}
