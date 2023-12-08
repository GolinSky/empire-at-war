using LightWeightFramework.Controller;
using WorkShop.LightWeightFramework.Game;

namespace WorkShop.LightWeightFramework.Service
{
    public abstract class Service:IService
    {
        string IEntity.Id => GetType().Name;

        void IEntity.Init(IGameObserver gameObserver)
        {
            OnInit(gameObserver);
        }

        void IEntity.Release()
        {
            Release();
        }

        protected abstract void OnInit(IGameObserver gameObserver);
        protected abstract void Release();
    }
}