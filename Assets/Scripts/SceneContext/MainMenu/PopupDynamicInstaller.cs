using EmpireAtWar.Services.Popup;
using EmpireAtWar.Ui.Popups;
using UnityEngine;
using LightWeightFramework.Components.Repository;
using Zenject;

namespace EmpireAtWar
{
    public class PopupDynamicInstaller : Installer
    {
        private readonly IRepository _repository;
        private readonly PopupType _popupType;

        public PopupDynamicInstaller(IRepository repository, PopupType popupType)
        {
            _repository = repository;
            _popupType = popupType;
        }
    
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<PopupUi>()
                .FromComponentInNewPrefab(_repository.Load<GameObject>($"{_popupType}{(nameof(PopupUi))}"))
                .AsSingle();
        }
    }
}
