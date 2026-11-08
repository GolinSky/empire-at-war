namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Online client: the host reported this entity's death; play it locally. Not used by gameplay.</summary>
    public interface IReplicaDeathFacade : IEntityFacade
    {
        void Die();
    }
}
