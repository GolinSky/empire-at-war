using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    public interface IHullScorchBlastPool
    {
        /// <summary>
        /// Plays one short blast at a hull-surface anchor and follows it, so new hull scorches do not just pop in.
        /// A null fog visibility means the unit is never hidden by the local fog of war.
        /// </summary>
        void Play(Transform anchor, IFogVisibilityFacade fogVisibility, float scale);
    }
}
