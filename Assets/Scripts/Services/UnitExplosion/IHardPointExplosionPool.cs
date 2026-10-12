using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    public interface IHardPointExplosionPool
    {
        /// <summary>
        /// Plays one hardpoint destruction explosion that follows the anchor while it plays.
        /// A null fog visibility means the unit is never hidden by the local fog of war.
        /// </summary>
        void Play(Transform anchor, IFogVisibilityFacade fogVisibility);

        void Stop(Transform anchor);
    }
}
