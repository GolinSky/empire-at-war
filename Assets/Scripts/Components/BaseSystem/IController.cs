namespace EmpireAtWar.Mvc
{
    public interface IController : IFrameworkObject
    {
        IModel GetModel();
    }
}
