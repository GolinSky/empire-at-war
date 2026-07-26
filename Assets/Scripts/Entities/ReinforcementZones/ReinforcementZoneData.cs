using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.ReinforcementZones
{
    [CreateAssetMenu(fileName = nameof(ReinforcementZoneData), menuName = "Data/Reinforcement Zone Data")]
    public sealed class ReinforcementZoneData : Data
    {
        [field: SerializeField, Min(0.01f)]
        public float CaptureSpeedPerNetShip { get; private set; } = 1f;
    }
}
