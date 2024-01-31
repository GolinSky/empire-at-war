using System.Collections.Generic;
using LightWeightFramework.Model;
using WorkShop.LightWeightFramework.Components;


namespace LightWeightFramework.Controller
{
    public abstract class Controller:IController
    {
        private List<IComponent> components = new List<IComponent>();

        public virtual string Id => GetType().Name;
        public abstract IModel GetModel();
        
        public IComponent AddComponent(IComponent component)
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

    }
    public abstract class Controller<TModel>:Controller, IController
        where TModel : IModel
    {
        protected TModel Model { get;  }

        protected Controller(TModel model)
        {
            Model = model;
        }

        public override IModel GetModel()
        {
            return Model;
        }
    }
}