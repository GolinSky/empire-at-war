namespace EmpireAtWar.Entities.DefendPlatform
{
    public sealed class DefendPlatformTypeFacade : IDefendPlatformTypeFacade
    {
        public DefendPlatformType Type { get; }

        public DefendPlatformTypeFacade(DefendPlatformType type) => Type = type;
    }
}
