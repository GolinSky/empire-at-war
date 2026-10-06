using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.BaseEntity
{
    public static class EntityDetection
    {
        public static bool IsCloaked(this IEntity entity) =>
            entity.TryGetFacade(out ICombatModifiersFacade combat) && combat.Modifiers.IsCloaked;

        /// <summary>
        /// True while the local player's fog of war hides the entity, exactly as it is drawn.
        /// Entities of the local team are never hidden.
        /// </summary>
        public static bool IsHiddenByFog(this IEntity entity) =>
            entity.TryGetFacade(out IFogVisibilityFacade fog) && fog.IsHiddenByFog;
    }
}
