using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Services.UnitExplosion
{
    public interface IHullScorchBlastPool
    {
        /// <summary>Seconds one unit waits after a blast before its next; stretches as the battle fills with blasts.</summary>
        float UnitInterval { get; }

        /// <summary>
        /// Plays one short blast at <paramref name="worldOffset"/> from a hull-surface anchor and follows it. False
        /// when skipped: hidden by fog, off camera, too small on screen, or over the battle-wide limits.
        /// A null fog visibility means the unit is never hidden by the local fog of war.
        /// </summary>
        bool TryPlay(Transform anchor, Vector3 worldOffset, IFogVisibilityFacade fogVisibility, float scale);
    }
}
