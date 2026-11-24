using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    public interface IMuzzleBlastPool
    {
        /// <summary>
        /// Plays one short blast of about <paramref name="diameter"/> world units at <paramref name="worldOffset"/>
        /// from a firing muzzle and follows it. A null fog visibility means the unit is never hidden by fog.
        /// </summary>
        void Play(Transform muzzle, Vector3 worldOffset, IFogVisibilityFacade fogVisibility, float diameter);
    }
}
