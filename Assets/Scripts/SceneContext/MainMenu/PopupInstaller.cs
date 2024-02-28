using EmpireAtWar.Services.Popup;
using EmpireAtWar.Ui.Popups;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class PopupInstaller : MonoInstaller
    {
        [SerializeField] private Transform popupParent;

        public override void InstallBindings()
        {
            Container
                .BindInstance(popupParent)
                .AsSingle()
                .WhenInjectedInto<PopupService>();
        
            Container
                .BindInterfacesTo<PopupService>()
                .AsSingle();
        
            Container.BindFactory<PopupType, PopupUi, PopupUiFacade>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<PopupDynamicInstaller>();
        }
    }
}
