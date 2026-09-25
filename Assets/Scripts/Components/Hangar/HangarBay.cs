using System;
using EmpireAtWar.Entities.Squadrons;
using UnityEngine;

namespace EmpireAtWar.Components.Hangar
{
    [Serializable]
    public struct HangarBay
    {
        [SerializeField] private SquadronType squadronType;
        [Tooltip("Squadrons this bay can launch during the whole battle.")]
        [SerializeField, Min(0)] private int reserve;
        [Tooltip("Squadrons from this bay that may be deployed at the same time.")]
        [SerializeField, Min(0)] private int maxActive;

        public HangarBay(SquadronType squadronType, int reserve, int maxActive)
        {
            this.squadronType = squadronType;
            this.reserve = reserve;
            this.maxActive = maxActive;
        }

        public SquadronType SquadronType => squadronType;
        public int Reserve => reserve;
        public int MaxActive => maxActive;
    }
}
