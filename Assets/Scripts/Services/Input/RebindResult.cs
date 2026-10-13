namespace EmpireAtWar.Services.Input
{
    public enum RebindResult
    {
        Completed = 0,
        Canceled = 1,
        Conflict = 2,
        /// <summary>A chord's key was pressed with another number of modifiers than the chord needs; nothing changed.</summary>
        WrongModifierCount = 3,
    }
}
