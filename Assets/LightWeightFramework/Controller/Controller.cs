using System.Collections.Generic;
using LightWeightFramework.Model;
using WorkShop.LightWeightFramework.Components;


namespace LightWeightFramework.Controller
{
    public abstract class Controller<TModel>:IController
        where TModel : IModel
    {
        private List<IComponent> components = new List<IComponent>();

        public virtual string Id => GetType().Name;
        protected TModel Model { get;  }
    

        IModelObserver IController.Model => Model;
     

        protected Controller(TModel model)
        {
            Model = model;
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
    }
}