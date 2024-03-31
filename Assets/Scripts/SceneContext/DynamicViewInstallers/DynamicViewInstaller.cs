using EmpireAtWar.Extentions;
using EmpireAtWar.Views.DefendPlatform;
using LightWeightFramework.Components;
using LightWeightFramework.Components.Repository;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public abstract class DynamicViewInstaller<TController, TModel, TView> : MonoInstaller
        where TController : Controller<TModel>
        where TModel : Model
        where TView: View
    {
        [SerializeField] private bool bindViewComponents;
        [SerializeField] private bool bindMonoComponent;

        protected View view;
        protected Vector3 startPosition;
        protected IRepository repository;

        protected virtual Transform ViewTransformParent => transform;

        protected virtual string ModelPathPrefix { get; } = string.Empty;
        protected virtual string ModelPathPostfix { get; } = string.Empty;
        
        protected virtual string ViewPathPrefix { get; } = string.Empty;
        protected virtual string ViewPathPostfix { get; } = string.Empty;


        [Inject]
        public void Constructor(IRepository repository, Vector3 startPosition)
        {
            this.repository = repository;
            this.startPosition = startPosition;
        }

        public sealed override void InstallBindings()
        {
            BindData();
            BindModel();
            BindController();
            BindComponents();
            BindView();
            AssignView();
            BindMonoComponent();
            BindViewComponents();
        }

        protected virtual void AssignView()
        {
            view = Container.Resolve<TView>();
        }

        protected virtual  void BindViewComponents()
        {
            if (bindViewComponents)
            {
                Container.Install<ViewComponentsInstaller>(new object[] { view });
            }
        }

        protected virtual  void BindMonoComponent()
        {
            if (bindMonoComponent)
            {
                Container.Install<MonoComponentInstaller>(new object[] { view.Transform });
            }
        }

        protected void BindData()
        {
            Container.BindEntity(startPosition);
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
            Container.BindModel<TModel>(repository, ModelPathPrefix, ModelPathPostfix);
        }

        protected virtual void BindView()
        {
            ViewDependencyBuilder
                .ConstructBuilder(Container)
                .AppendToPath(ViewPathPrefix, ViewPathPostfix)
                .BindFromNewComponent<TView>(repository, ViewTransformParent);
        }
    }
}