using EmpireAtWar.ViewComponents;
using EmpireAtWar.Views.ViewImpl;
using Zenject;

namespace EmpireAtWar
{
    public class ModelDependencyInstaller:Installer
    {
        private readonly View _view;

        public ModelDependencyInstaller(View view)
        {
            _view = view;
        }
        public override void InstallBindings()
        {
            ModelDependency[] viewModels = _view.ModelDependencies;
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