using EmpireAtWar.Services.Popup;
using EmpireAtWar.Ui.Popups;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class PopupInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .BindInterfacesTo<PopupService>()
                .AsSingle();
        
            Container
                .BindFactory<PopupType, Transform, PopupUi, PopupUiFacade>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<PopupDynamicInstaller>();
        }
    }
}
