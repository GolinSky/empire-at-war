using EmpireAtWar.Views.ViewImpl;
using LightWeightFramework.Components.ViewComponents;
using Zenject;

namespace EmpireAtWar
{
    public class ViewComponentsInstaller : Installer
    {
       private readonly View _view;

       public ViewComponentsInstaller(View view)
       {
           _view = view;
       }
       
        public override void InstallBindings()
        {
            foreach (ViewComponent component in _view.ViewComponents)
            {
                Container.Inject(component);
                Container
                    .BindInterfacesTo(component.GetType())
                    .FromComponentOn(component.gameObject)
                    .AsSingle();
            }
        }
    }
}