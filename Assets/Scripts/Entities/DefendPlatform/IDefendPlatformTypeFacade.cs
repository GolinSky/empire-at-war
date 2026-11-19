using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Entities.DefendPlatform
{
    /// <summary>Which platform or satellite this entity is; the AI rates its guns by type.</summary>
    public interface IDefendPlatformTypeFacade : IEntityFacade
    {
        DefendPlatformType Type { get; }
    }
}
