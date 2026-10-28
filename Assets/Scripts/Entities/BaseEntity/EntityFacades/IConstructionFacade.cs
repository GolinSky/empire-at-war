using System;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IConstructionFacade : IEntityFacade
    {
        float Progress { get; }
        float BuildTime { get; }
        bool IsConstructing { get; }
        event Action<float> ProgressChanged;
        void Restore(float progress, float buildTime);
    }
}
