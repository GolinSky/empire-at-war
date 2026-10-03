using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Components.AttackComponent
{
    public interface IAttackDataFactory
    {
        AttackData ConstructData(IEntity entity, HardPointType hardPointType = HardPointType.Any);

        AttackData ConstructHardPointData(IEntity entity, int hardPointId);
    }
    public class AttackDataFactory: IAttackDataFactory
    {
        public AttackData ConstructData(IEntity entity, HardPointType hardPointType = HardPointType.Any)
        {
            return new AttackData(
                entity.HealthModel,
                entity.GetFacade<IHealthFacade>(),
                hardPointType);
        }

        public AttackData ConstructHardPointData(IEntity entity, int hardPointId)
        {
            return new AttackData(
                entity.HealthModel,
                entity.GetFacade<IHealthFacade>(),
                entity.GetFacade<IHardPointsFacade>().HardPoints[hardPointId]);
        }
    }
}
