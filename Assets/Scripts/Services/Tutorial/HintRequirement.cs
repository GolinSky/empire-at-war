using System;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>A state a step needs. While it is missing, the reusable fix step shows instead.</summary>
    [Serializable]
    public struct HintRequirement
    {
        [SerializeField] private HintStateId state;
        [Tooltip("Reusable small step shown while the state is missing, such as Select a unit. Never recorded as done.")]
        [SerializeField] private HintDefinition step;

        public HintStateId State => state;
        public HintDefinition Step => step;
    }
}
