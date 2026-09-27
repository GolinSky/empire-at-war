using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using UnityEngine.Rendering;

namespace EmpireAtWar.Components.Radar
{
    public interface IRadarModelObserver : IModelObserver
    {
        float Range { get; }
        float Delay { get; }
        ObservableList<IEntity> Enemies { get; }
    }
}
