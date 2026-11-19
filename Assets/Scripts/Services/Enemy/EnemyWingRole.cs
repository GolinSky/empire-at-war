namespace EmpireAtWar.Services.Enemy
{
    /// <summary>What a wing group of the AI is for.</summary>
    public enum EnemyWingRole
    {
        /// <summary>Group A: holds the centre; pads (base-side) → mines → relay; retakes and stays.</summary>
        Centre,
        /// <summary>Group B: own-side outer zone, pad → relay → mine; then the next outer zone; then raids mines.</summary>
        Outer
    }
}
