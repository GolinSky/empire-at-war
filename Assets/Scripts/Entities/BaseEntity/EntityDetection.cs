using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;

namespace EmpireAtWar.Entities.BaseEntity
{
    public static class EntityDetection
    {
        public static bool IsCloaked(this IEntity entity) =>
            entity.TryGetFacade(out ICombatModifiersFacade combat) && combat.Modifiers.IsCloaked;

        public static bool IsEntityVisible(this IVisionService vision, PlayerId viewer, IEntity entity) =>
            !entity.IsCloaked() && vision.IsVisible(viewer,
                entity.GetFacade<IEntityTransformFacade>().Transform.position);
    }
}
