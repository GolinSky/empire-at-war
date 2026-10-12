namespace EmpireAtWar.Entities.BaseEntity
{
    /// <summary>Part of a pooled entity that redoes its initialization when the entity is reused.</summary>
    public interface IRespawnable
    {
        void Respawn();
    }
}
