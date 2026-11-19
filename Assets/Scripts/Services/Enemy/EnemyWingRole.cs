namespace EmpireAtWar.Services.Enemy
{
    /// <summary>What a wing group of the AI is for.</summary>
    public enum EnemyWingRole
    {
        /// <summary>Group A: holds the centre; lands on its mining spots (base-side first); retakes and stays.</summary>
        Centre,
        /// <summary>
        /// Group B: one per outer zone, landing on the zone's mining spot; once its zone is taken, the next outer
        /// zone; with none left it raids mines.
        /// </summary>
        Outer
    }
}
