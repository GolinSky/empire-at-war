using System.Collections.Generic;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    public interface IUnitExplosionService : IService
    {
        /// <summary>Plays an explosion of the given kind that covers the hull renderers' bounds.</summary>
        void Spawn(IReadOnlyList<Renderer> hullRenderers, UnitExplosionKind kind, float coverageScale = 1f);
    }
}
