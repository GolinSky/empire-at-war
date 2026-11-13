using System;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    [Serializable]
    public sealed class PlanetDecorationEntry
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField, Min(0)] public int MinimumCount { get; private set; }
        [field: SerializeField, Min(0)] public int MaximumCount { get; private set; }
        [field: SerializeField, Min(0.01f)] public float MinimumScale { get; private set; } = 1f;
        [field: SerializeField, Min(0.01f)] public float MaximumScale { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float HeightJitter { get; private set; }
        [field: SerializeField, Min(0f)] public float ClearanceRadius { get; private set; }
    }
}
