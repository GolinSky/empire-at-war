using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Vision
{
    public static class VisionQueries
    {
        public static bool IsVisible(this IVisionService vision, PlayerId viewer, IEntity entity) =>
            entity.TryGetFacade(out IVisionFootprintFacade footprint)
                ? vision.IsVisible(viewer, footprint)
                : vision.IsRevealed(viewer, entity.Id) ||
                  vision.IsVisible(viewer, entity.GetFacade<IEntityTransformFacade>().Transform.position);
    }
}
