namespace WorkShop.LightWeightFramework.Service
{
    public interface IServiceHub
    {
        TService Get<TService>() where TService : IService;
    }
}