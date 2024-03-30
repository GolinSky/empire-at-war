using LightWeightFramework.Components;
using LightWeightFramework.Components.Repository;
using LightWeightFramework.Components.ViewComponents;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Extentions
{
    public class ViewDependencyBuilder:DependencyBuilder<ViewDependencyBuilder>
    {
        private ViewDependencyBuilder(DiContainer container) : base(container)
        {
        }
        
        public void BindFromNewComponent<TView>(IRepository repository, bool notInjectViewComponents = false)
            where TView : IView
        {
            ConstructName<TView>();

            if (notInjectViewComponents)
            {
                Container
                    .BindInterfacesAndSelfTo<TView>()
                    .FromComponentInNewPrefab(repository.Load<GameObject>(PathToFile))
                    .AsSingle();
            }
            else
            {
                Container
                    .BindInterfacesAndSelfTo<TView>()
                    .FromComponentInNewPrefab(repository.Load<GameObject>(PathToFile))
                    .AsSingle()
                    .OnInstantiated(HandleCreation<TView>);
            }
   
        }

        public void BindFromInstance<TView>(object view)
            where TView : IView
        {
            Container
                .BindInterfacesTo<TView>()
                .FromInstance(view)
                .AsSingle();
        }

        private void HandleCreation<TView>(InjectContext context, object o) where TView : IView
        {
            TView view = (TView)o;
            DiContainer contextContainer = context.Container;
            foreach (ViewComponent component in view.ViewComponents)
            {
                contextContainer.Inject(component);
                contextContainer
                    .BindInterfacesTo(component.GetType())
                    .FromComponentOn(component.gameObject)
                    .AsSingle();
            }
        }

        
        public static ViewDependencyBuilder ConstructBuilder(DiContainer container)
        {
            return new ViewDependencyBuilder(container);
        }
    }
}