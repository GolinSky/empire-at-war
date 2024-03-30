using LightWeightFramework.Components.Repository;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Extentions
{
    public class ModelDependencyBuilder:DependencyBuilder<ModelDependencyBuilder>
    {
        private ModelDependencyBuilder(DiContainer container) : base(container)
        {
        }

        public DiContainer BindFromNewScriptable<TModel>(IRepository repository) 
            where TModel : Model
        {
            ConstructName<TModel>();
            
            Container
                .BindInterfacesAndSelfTo<TModel>()
                .FromNewScriptableObject(repository.Load<TModel>(PathToFile))
                .AsSingle()
                .OnInstantiated(HandleModel<TModel>);
            return Container;
        }
         
        private void HandleModel<TModel>(InjectContext context, object @object)
        {
            Model model = (Model)@object;
            foreach (IModel currentModel in model.CurrentModels)
            {
                context.Container.Inject(currentModel);
            }
        }
        
        public static ModelDependencyBuilder ConstructBuilder(DiContainer container)
        {
            return new ModelDependencyBuilder(container);
        }
    }
}