namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Where an opening capture detachment is aimed: the nearest zone away from the centre, or the centre.</summary>
    public enum EnemyOpeningFocus
    {
        None = 0,
        Neighbour = 1,
        Centre = 2
    }
}
