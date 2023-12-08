using System.Collections.Generic;
using LightWeightFramework.Model;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Components;
using WorkShop.LightWeightFramework.Game;
using WorkShop.LightWeightFramework.Service;

namespace LightWeightFramework.Controller
{
    public abstract class Controller<TModel>:IController
        where TModel : IModel
    {
        private List<IComponent> components = new List<IComponent>();

        public virtual string Id => GetType().Name;
        protected TModel Model { get;  }
    

        IModelObserver IController.Model => Model;
        protected IGameObserver GameObserver { get; private set; }

        protected Controller(TModel model)
        {
            Model = model;
        }

        public void Init(IGameObserver gameObserver)
        {
            GameObserver = gameObserver;
            OnBeforeComponentsInitialed();
            foreach (var component in components)
            {
                component.Init(gameObserver);
            }
            OnInit();
        }

        public void Release()
        {
            OnRelease();
        }
        

        protected IComponent AddComponent(IComponent component)
        {
            components.Add(component);
            return component;
        }
        
        protected TComponent AddComponent<TComponent>(TComponent component) where TComponent : IComponent
        {
            components.Add(component);
            return component;
        }
        
        public TComponent GetComponent<TComponent>() where TComponent : IComponent
        {
            return GetComponentInternal<TComponent>();
        }

        private T GetComponentInternal<T>() // make extension 
        {
            foreach (var component in components)
            {
                if (component is T targetComponent)
                {
                    return targetComponent;
                }
            }

            return default;
        }

        protected TService GetService<TService>() where TService:IService
        {
            return GameObserver.ServiceHub.Get<TService>();
        }
        public virtual ICommand ConstructCommand()
        {
            throw new System.NotImplementedException();
        }
        
        protected virtual void OnInit(){}

        protected virtual void OnBeforeComponentsInitialed()
        {
   
        }
        protected virtual void OnRelease(){}
    }
}