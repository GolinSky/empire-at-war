using WorkShop.LightWeightFramework.Game;

namespace LightWeightFramework.Controller
{
    public interface IEntity
    {
        void Init(IGameObserver gameObserver);
        void Release();
        string Id { get; }
    }
}