using System;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    [Serializable]
    public sealed class SiteFacilityCost
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField, Min(0f)] public float Price { get; private set; }
        [field: SerializeField, Min(0.1f)] public float BuildTime { get; private set; }
    }
}
