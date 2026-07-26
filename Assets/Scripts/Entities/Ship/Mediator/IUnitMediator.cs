using EmpireAtWar.Entities.BaseEntity;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    public interface IUnitComponent
    {
        void SetMediator(IUnitMediator mediator);
    }

    public interface IUnitMediator
    {
        void HandleNewEnemy(IEntity entity);
        void HandleRadarContacts(IReadOnlyList<RadarContact> contacts);
        void OnSelect(bool isActive);
    }
}
