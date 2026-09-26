using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Components.AttackComponent
{
    public interface IAttackDataFactory
    {
        AttackData ConstructData(IEntity entity, HardPointType hardPointType = HardPointType.Any);
    }
    public class AttackDataFactory: IAttackDataFactory
    {
        public AttackData ConstructData(IEntity entity, HardPointType hardPointType = HardPointType.Any)
        {
            if (entity.TryGetFacade(out IHealthFacade healthFacade))
            {
                AttackData attackData = new AttackData(
                    entity.HealthModel,
                    healthFacade,
                    hardPointType);
                return attackData;
            }
            return null;
        }
    }
}
