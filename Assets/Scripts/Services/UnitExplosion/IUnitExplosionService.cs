using System.Collections.Generic;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    public interface IUnitExplosionService : IService
    {
        void Spawn(IReadOnlyList<Renderer> hullRenderers);
    }
}
