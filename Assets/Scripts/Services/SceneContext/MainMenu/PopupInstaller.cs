using EmpireAtWar.Entities.MenuUi.Popups;
using Zenject;

namespace EmpireAtWar
{
    public class PopupInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<SkirmishPopupModel>().AsSingle();
            Container.Bind<SettingsPopupModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<PopupUiController>().AsSingle();
        }
    }
}
