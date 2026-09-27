using System;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Entities.Map
{
    [Serializable]
    public sealed class RockLayerSettings
    {
        [field: SerializeField, Min(0f)] public float Share { get; private set; }
        [field: SerializeField] public FloatRange Scale { get; private set; }
        [field: SerializeField, Min(0f), Tooltip("Largest vertical offset from the battle plane.")]
        public float HeightJitter { get; private set; }
        [field: SerializeField, Tooltip("Tumbles the rock on every axis instead of only turning it.")]
        public bool IsTumbled { get; private set; }
    }
}
