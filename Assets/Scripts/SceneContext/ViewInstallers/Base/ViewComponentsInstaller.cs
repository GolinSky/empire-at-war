using LightWeightFramework.Components;
using LightWeightFramework.Components.ViewComponents;
using Zenject;

namespace EmpireAtWar
{
    public class ViewComponentsInstaller : Installer
    {
       private readonly View view;

       public ViewComponentsInstaller(View view)
       {
           this.view = view;
       }
       
        public override void InstallBindings()
        {
            foreach (ViewComponent component in view.ViewComponents)
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