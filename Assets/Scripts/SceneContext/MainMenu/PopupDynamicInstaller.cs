using EmpireAtWar.Services.Popup;
using EmpireAtWar.Ui.Popups;
using UnityEngine;
using LightWeightFramework.Components.Repository;
using Zenject;

namespace EmpireAtWar
{
    public class PopupDynamicInstaller : Installer
    {
        private readonly IRepository repository;
        private readonly PopupType popupType;

        public PopupDynamicInstaller(IRepository repository, PopupType popupType)
        {
            this.repository = repository;
            this.popupType = popupType;
        }
    
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<PopupUi>()
                .FromComponentInNewPrefab(repository.Load<GameObject>($"{popupType}{(nameof(PopupUi))}"))
                .AsSingle();
        }
    }
}
