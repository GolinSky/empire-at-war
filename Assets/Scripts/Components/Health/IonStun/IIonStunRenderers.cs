using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    /// <summary>Read-only renderers of a spawned ion stun effect, for systems that hide it with its unit.</summary>
    public interface IIonStunRenderers
    {
        IEnumerable<Renderer> Renderers { get; }
    }
}
