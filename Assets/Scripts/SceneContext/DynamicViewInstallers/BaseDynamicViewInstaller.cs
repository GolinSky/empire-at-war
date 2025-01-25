using EmpireAtWar.Extentions;
using EmpireAtWar.Views.ViewImpl;
using LightWeightFramework.Components.Repository;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    //todo: check duplicate issue with DynamicViewInstaller
    public abstract class BaseDynamicViewInstaller<TController, TModel, TView> : MonoInstaller
        where TController : Controller<TModel>
        where TModel : Model
        where TView : View
    {
        [SerializeField] private bool bindViewComponents;
        [SerializeField] private bool bindMonoComponent;

        protected View View { get; private set; }
        protected IRepository Repository { get; private set; }

        protected virtual Transform ViewTransformParent => transform;

        protected virtual string ModelPathPrefix { get; } = string.Empty;
        protected virtual string ModelPathPostfix { get; } = string.Empty;

        protected virtual string ViewPathPrefix { get; } = string.Empty;
        protected virtual string ViewPathPostfix { get; } = string.Empty;


        [Inject]
        public void Constructor(IRepository repository)
        {
            Repository = repository;
        }

        public sealed override void InstallBindings()
        {
            BindParameters();
            BindModel();
            BindController();
            BindComponents();
            BindView();
            AssignView();
            BindMonoComponents();
            BindViewComponents();
        }

  
        protected virtual void AssignView()
        {
            View = Container.Resolve<TView>();
        }

        protected virtual void BindViewComponents()
        {
            if (bindViewComponents)
            {
                Container.Install<ViewComponentsInstaller>(new object[] { View });
            }
        }

        protected virtual void BindMonoComponents()
        {
            if (bindMonoComponent)
            {
                Container.Install<MonoComponentInstaller>(new object[] { View.Transform });
            }
        }

        protected virtual void BindParameters() {}
        
        protected virtual void BindComponents() {}

        protected virtual void BindController()
        {
            Container.BindInterfaces<TController>();
        }

        protected virtual void BindModel()
        {
            Container.BindModel<TModel>(Repository, ModelPathPrefix, ModelPathPostfix);
        }

        protected virtual void BindView()
        {
            ViewDependencyBuilder
                .ConstructBuilder(Container)
                .AppendToPath(ViewPathPrefix, ViewPathPostfix)
                .BindFromNewComponent<TView>(Repository, ViewTransformParent);
        }
    }
}