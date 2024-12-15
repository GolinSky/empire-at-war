using EmpireAtWar.Extentions;
using EmpireAtWar.Views.ViewImpl;
using LightWeightFramework.Components;
using LightWeightFramework.Components.Repository;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;
using View = EmpireAtWar.Views.ViewImpl.View;

namespace EmpireAtWar
{
    public abstract class DynamicViewInstaller<TController, TModel, TView> : MonoInstaller
        where TController : Controller<TModel>
        where TModel : Model
        where TView: View
    {
        [SerializeField] private bool bindViewComponents;
        [SerializeField] private bool bindMonoComponent;

        protected View View { get; private set; }
        protected Vector3 StartPosition { get; private set; }
        protected IRepository Repository { get; private set; }

        protected virtual Transform ViewTransformParent => transform;

        protected virtual string ModelPathPrefix { get; } = string.Empty;
        protected virtual string ModelPathPostfix { get; } = string.Empty;
        
        protected virtual string ViewPathPrefix { get; } = string.Empty;
        protected virtual string ViewPathPostfix { get; } = string.Empty;


        [Inject]
        public void Constructor(IRepository repository, Vector3 startPosition)
        {
            Repository = repository;
            StartPosition = startPosition;
        }

        public sealed override void InstallBindings()
        {
            BindData();
            BindModel();
            BindController();
            BindComponents();
            BindView();
            AssignView();
            BindModelDependency();
            BindMonoComponent();
            BindViewComponents();
        }

        protected virtual void AssignView()
        {
            View = Container.Resolve<TView>();
            OnViewCreated();
        }
        
        protected virtual void BindModelDependency()
        {
            Container.Install<ModelDependencyInstaller>(new object[] { View });
        }

        protected virtual void BindViewComponents()
        {
            if (bindViewComponents)
            {
                Container.Install<ViewComponentsInstaller>(new object[] { View });
            }
        }

        protected virtual void BindMonoComponent()
        {
            if (bindMonoComponent)
            {
                Container.Install<MonoComponentInstaller>(new object[] { View.Transform });
            }
        }

        protected void BindData()// BindParameters
        {
            Container.BindEntity(StartPosition);
            OnBindData();
        }
        protected virtual void OnBindData() {}
        protected virtual void BindComponents(){}

        protected virtual void BindController()
        {
            Container.BindInterfaces<TController>();
        }

        protected virtual void BindModel()
        {
            ModelDependencyBuilder
                .ConstructBuilder(Container)
                .AppendToPath(ModelPathPrefix, ModelPathPostfix)
                .BindFromNewScriptable<TModel>(Repository, OnModelCreated);
        }

        protected virtual void BindView()
        {
            ViewDependencyBuilder
                .ConstructBuilder(Container)
                .AppendToPath(ViewPathPrefix, ViewPathPostfix)
                .BindFromNewComponent<TView>(Repository, ViewTransformParent);
        }

        protected virtual void OnModelCreated() {}

        protected virtual void OnViewCreated() {}
    }
}