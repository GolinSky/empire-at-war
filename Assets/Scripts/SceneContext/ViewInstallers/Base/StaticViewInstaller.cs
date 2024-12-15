using EmpireAtWar.Extentions;
using EmpireAtWar.Views.ViewImpl;
using LightWeightFramework.Components.Repository;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public abstract class StaticViewInstaller<TController, TModel> : MonoInstaller
        where TController : Controller
        where TModel : Model
    {
        [SerializeField] private View view;
        [SerializeField] private bool bindViewComponents;
        [SerializeField] private bool bindMonoComponent;
        
        [Inject]
        protected IRepository Repository { get; }
        
        private void OnValidate()
        {
            view = GetComponent<View>();
        }
        
        public override void InstallBindings()
        {
            if (bindMonoComponent)
            {
                Container.Install<MonoComponentInstaller>(new object[]{transform});
            }
            
            BindModel();
            BindController();
            
            Container
                .BindInterfacesAndSelfTo(view.GetType())
                .FromInstance(view)
                .AsSingle();

            if (bindViewComponents)
            {
                Container.Install<ViewComponentsInstaller>(new object[] { view });
            }
        }

        protected virtual void BindController()
        {
            Container.BindInterfaces<TController>();
        }

        protected virtual void BindModel()
        {
            Container.BindModel<TModel>(Repository);
        }
    }
}