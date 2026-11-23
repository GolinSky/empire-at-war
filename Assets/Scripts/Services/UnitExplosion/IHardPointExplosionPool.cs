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

        /// <summary>
        /// Puts out the anchor's fire on a healed unit. It keeps following the anchor while the last flames die,
        /// then returns to the pool; <paramref name="immediately"/> clears it at once (loaded save).
        /// </summary>
        void Extinguish(Transform anchor, bool immediately);
    }
}
