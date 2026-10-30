namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IEntityHighlightFacade : IEntityFacade
    {
        void SetHovered(bool isHovered);
        void SetAttackTargeted(bool isAttackTargeted);
    }
}
