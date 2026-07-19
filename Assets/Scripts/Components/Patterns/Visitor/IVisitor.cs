namespace EmpireAtWar.Patterns.Visitor
{
    public interface IVisitor<T>
    {
        void Handle(T handler);
    }
}