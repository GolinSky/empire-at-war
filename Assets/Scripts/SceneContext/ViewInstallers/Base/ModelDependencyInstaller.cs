using EmpireAtWar.ViewComponents;
using EmpireAtWar.Views.ViewImpl;
using Zenject;

namespace EmpireAtWar
{
    public class ModelDependencyInstaller:Installer
    {
        private readonly View view;

        public ModelDependencyInstaller(View view)
        {
            this.view = view;
        }
        public override void InstallBindings()
        {
            ModelDependency[] viewModels = view.ModelDependencies;
            foreach (ModelDependency viewModel in viewModels)
            {
                Container.Inject(viewModel);
                Container
                    .BindInterfacesTo(viewModel.GetType())
                    .FromComponentOn(viewModel.gameObject)
                    .AsSingle();
            }
        }
    }
}