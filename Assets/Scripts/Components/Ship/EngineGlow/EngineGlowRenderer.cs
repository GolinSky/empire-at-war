using System;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.EngineGlow
{
    /// <summary>One engine glow and the engine hardpoint whose destruction switches it off.</summary>
    [Serializable]
    public struct EngineGlowRenderer
    {
        /// <summary>No engine hardpoint owns the glow: it only follows the ship's engine state.</summary>
        public const int NO_ENGINE = -1;

        [SerializeField] private MeshRenderer renderer;
        // Index in HealthComponent.ShipUnits, which is also the hardpoint model id.
        [SerializeField] private int engineHardPointIndex;

        public EngineGlowRenderer(MeshRenderer renderer, int engineHardPointIndex)
        {
            this.renderer = renderer;
            this.engineHardPointIndex = engineHardPointIndex;
        }

        public MeshRenderer Renderer => renderer;
        public int EngineHardPointIndex => engineHardPointIndex;
    }
}
