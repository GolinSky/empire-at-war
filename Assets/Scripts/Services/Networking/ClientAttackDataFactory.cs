using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Online client: attack data whose hits never damage (see <see cref="ReplicaHealthFacade"/>).</summary>
    public sealed class ClientAttackDataFactory : IAttackDataFactory
    {
        public AttackData ConstructData(IEntity entity, HardPointType hardPointType = HardPointType.Any) =>
            new AttackData(entity.HealthModel, ReplicaHealthFacade.Instance, hardPointType, entity);

        public AttackData ConstructHardPointData(IEntity entity, int hardPointId) =>
            new AttackData(entity.HealthModel, ReplicaHealthFacade.Instance,
                entity.GetFacade<IHardPointsFacade>().HardPoints[hardPointId], entity);
    }
}
