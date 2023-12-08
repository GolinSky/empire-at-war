using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using WorkShop.LightWeightFramework;
using WorkShop.LightWeightFramework.Command;
using Zenject;

namespace EmpireAtWar.Extentions
{
    public static class InstallerExtensions
    {
        public static void BindEntity<TController, TView, TModel, TCommand>(this DiContainer container, TModel model, TView view)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
            where TCommand : Command
        {
            container.BindInterfacesAndSelfTo<TCommand>()
                .AsTransient();

            container
                .BindInterfacesAndSelfTo<TModel>()
                .FromInstance(model)
                .AsTransient();

            container
                .BindInterfacesTo<TView>()
                .FromInstance(view)
                .AsTransient();

            container
                .BindInterfacesAndSelfTo<TController>()
                .AsTransient();
        }
        
        public static void BindEntityNoCommand<TController, TView, TModel>(this DiContainer container, TModel model, TView view)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
        {
            container
                .BindInterfacesAndSelfTo<TModel>()
                .FromInstance(model)
                .AsSingle();

            container
                .BindInterfacesTo<TView>()
                .FromInstance(view)
                .AsSingle();

            container
                .BindInterfacesAndSelfTo<TController>()
                .AsSingle();
        }
    }
}