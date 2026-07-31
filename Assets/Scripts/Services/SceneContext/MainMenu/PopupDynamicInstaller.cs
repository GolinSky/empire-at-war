using EmpireAtWar.Services.Popup;
using EmpireAtWar.Ui.Popups;
using UnityEngine;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar
{
    public class PopupDynamicInstaller : Installer
    {
        private readonly IRepository _repository;
        private readonly PopupType _popupType;
        private readonly Transform _popupParent;

        public PopupDynamicInstaller(
            IRepository repository,
            PopupType popupType,
            Transform popupParent)
        {
            _repository = repository ?? throw new System.ArgumentNullException(nameof(repository));
            _popupType = popupType;
            _popupParent = popupParent != null
                ? popupParent
                : throw new System.ArgumentNullException(nameof(popupParent));
        }
    
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<PopupUi>()
                .FromComponentInNewPrefab(_repository.Load<GameObject>($"{_popupType}{(nameof(PopupUi))}"))
                .UnderTransform(_popupParent)
                .AsSingle();
        }
    }
}
