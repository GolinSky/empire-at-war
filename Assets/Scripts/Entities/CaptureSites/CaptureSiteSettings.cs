using System;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    [Serializable]
    public sealed class CaptureSiteSettings
    {
        [field: SerializeField, Min(1f)] public float Radius { get; private set; }
        [field: SerializeField, Min(1f)] public float CaptureDuration { get; private set; }
        [field: SerializeField] public float FacilityHeightOffset { get; private set; }
    }
}
