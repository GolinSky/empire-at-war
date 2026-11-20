using System.Collections.Generic;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    public interface IUnitExplosionService : IService
    {
        void Spawn(IReadOnlyList<Renderer> hullRenderers, float coverageScale = 1f);
        void Spawn(Vector3 position, float diameter);
    }
}
