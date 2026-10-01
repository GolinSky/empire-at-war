using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SpaceStation;

namespace EmpireAtWar.Entities.Units
{
    public static class EntityRoles
    {
        public static bool IsUnit(this IEntity entity) => entity.TryGetFacade(out IUnitTypeFacade _);

        public static bool IsShip(this IEntity entity) =>
            entity.TryGetFacade(out IUnitTypeFacade unit) && unit.UnitTypeId.IsShip;

        public static bool IsSquadron(this IEntity entity) =>
            entity.TryGetFacade(out IUnitTypeFacade unit) && unit.UnitTypeId.IsSquadron;

        public static bool IsPlayerBase(this IEntity entity) => entity.TryGetFacade(out IPlayerBaseFacade _);

        public static bool IsSameUnitType(this IEntity entity, IEntity other) =>
            entity.TryGetFacade(out IUnitTypeFacade unit) &&
            other.TryGetFacade(out IUnitTypeFacade otherUnit) &&
            unit.UnitTypeId == otherUnit.UnitTypeId;

        public static bool IsUnitType(this IEntity entity, UnitTypeId unitTypeId) =>
            entity.TryGetFacade(out IUnitTypeFacade unit) && unit.UnitTypeId == unitTypeId;
    }
}
