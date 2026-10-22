using System;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IHealthAudioEvents
    {
        event Action<Vector3> ShieldImpacted;
        event Action HealthRestored;
    }
}
