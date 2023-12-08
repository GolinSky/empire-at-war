using LightWeightFramework.Controller;
using WorkShop.LightWeightFramework.Game;

namespace WorkShop.LightWeightFramework.Components
{
    public abstract class Component : IComponent
    {
        string IEntity.Id => GetType().Name;
        protected IGameObserver GameObserver { get; private set; }


        void IEntity.Init(IGameObserver gameObserver)
        {
            GameObserver = gameObserver;
            OnInit(gameObserver);
        }
        
        void IEntity.Release()
        {
            OnRelease();
        }

        protected abstract void OnInit(IGameObserver gameObserver);
        protected abstract void OnRelease();
    }
}