namespace EmpireAtWar.Entities.BaseEntity
{
    /// <summary>Part of a pooled entity that leaves the battle when the dead entity returns to its pool.</summary>
    public interface IDespawnable
    {
        void Despawn();
    }
}
