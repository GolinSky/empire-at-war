namespace EmpireAtWar.Services.Input
{
    public enum ConflictResolution
    {
        /// <summary>Unbinds every conflicting slot and keeps the new binding.</summary>
        Replace = 0,
        /// <summary>Gives the single conflicting slot the previous binding.</summary>
        Swap = 1,
        /// <summary>Restores the previous binding.</summary>
        Cancel = 2,
    }
}
