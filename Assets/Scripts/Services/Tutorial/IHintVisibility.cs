namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Whether hint cards may show. Conditions keep listening while this is off.</summary>
    public interface IHintVisibility
    {
        bool IsOn { get; }
    }
}
