namespace EmpireAtWar.Services.Battle
{
    /// <summary>Local units can be commanded; other units (allies and enemies) can only be inspected.</summary>
    public enum SelectionScope
    {
        Local = 0,
        Other = 1
    }
}
