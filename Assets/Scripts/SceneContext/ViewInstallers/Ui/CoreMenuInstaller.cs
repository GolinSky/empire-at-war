using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Models.Menu;
using Zenject;

namespace EmpireAtWar.SceneContext.ViewInstallers.Ui
{
    public class CoreMenuInstaller:StaticViewInstaller<MenuController, MenuModel>
    {
        [Inject]
        private Zenject.SceneContext SceneContext { get; }

        protected override void BindController()
        {
            base.BindController();
            SceneContext.Container.Bind<IUserStateNotifier>()
                .FromMethod((() => Container.Resolve<IUserStateNotifier>()));
        }
    }
}