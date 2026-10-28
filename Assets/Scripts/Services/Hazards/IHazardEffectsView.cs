using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Map.Hazards;

namespace EmpireAtWar.Services.Hazards
{
    public interface IHazardEffectsView
    {
        void ShowHazardEntry(IEntity entity, SpaceHazardData data);
    }
}
