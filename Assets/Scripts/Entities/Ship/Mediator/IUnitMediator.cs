using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    public interface IUnitComponent
    {
        void SetMediator(IUnitMediator mediator);
    }

    public interface IUnitMediator
    {
        void HandleNewEnemy(IEntity entity);
        void OnSelect(bool isActive);
    }
}
