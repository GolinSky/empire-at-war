using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
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
            if (entity.TryGetCommand(out IHealthCommand healthCommand))
            {
                AttackData attackData = new AttackData(
                    entity.Model.GetModelObserver<IHealthModelObserver>(),
                    healthCommand,
                    hardPointType);
                return attackData;
            }
            return null;
        }
    }
}