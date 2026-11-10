namespace EmpireAtWar.Services.Tutorial.Guided
{
    /// <summary>The player chose the tutorial, so its cards show even when "Show tutorial hints" is off.</summary>
    public sealed class AlwaysOnHintVisibility : IHintVisibility
    {
        public bool IsOn => true;
    }
}
