using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    public interface ISquadronIconCommand : IEntityCommand
    {
        bool ContainsScreenPoint(Vector2 screenPoint);
    }
}
