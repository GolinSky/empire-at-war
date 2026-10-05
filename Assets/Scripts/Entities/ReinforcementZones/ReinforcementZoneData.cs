using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.ReinforcementZones
{
    [CreateAssetMenu(fileName = nameof(ReinforcementZoneData), menuName = "Data/Reinforcement Zone Data")]
    public sealed class ReinforcementZoneData : Data
    {
        [field: SerializeField, Min(0.01f)]
        public float CaptureSpeedPerNetShip { get; private set; } = 1f;

        /// <summary>Capture strength of one fighter squadron relative to one ship.</summary>
        [field: SerializeField, Range(0f, 1f)]
        public float SquadronCaptureWeight { get; private set; } = 0.5f;

        /// <summary>Radius of a player's home area: the old default zone, now only a rally and AI arrival anchor.</summary>
        [field: SerializeField, Min(1f)]
        public float HomeAreaRadius { get; private set; } = 270f;

        /// <summary>Radius around a relay where only its owner's team may spawn reinforcements.</summary>
        [field: SerializeField, Min(0f)]
        public float RelaySpawnBlockRadius { get; private set; } = 900f;

        /// <summary>Spawn-block distance added around every asteroid obstacle's footprint.</summary>
        [field: SerializeField, Min(0f)]
        public float AsteroidSpawnBlockMargin { get; private set; } = 60f;
    }
}
