using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    public interface IHardPointExplosionPool
    {
        /// <summary>
        /// Plays one hardpoint destruction explosion at the supplied world scale, following its VFX anchor.
        /// A null fog visibility means the unit is never hidden by the local fog of war.
        /// </summary>
        void Play(Transform anchor, IFogVisibilityFacade fogVisibility, float scale);

        void Stop(Transform anchor);
    }
}
