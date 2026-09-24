namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IFocusFireCommand : IEntityCommand
    {
        void FocusFire(IEntity target);
    }
}
