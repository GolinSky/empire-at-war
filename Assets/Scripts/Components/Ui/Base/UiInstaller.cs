using System;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Ui.Base
{
    public class UiInstaller: Installer
    {
        private const string DEFAULT_NAME = "Ui";
        private IAssetService _assetService;
        private UiType _uiType;
        private Transform _parent;
        
        [Inject]
        public void Constructor(IAssetService assetService, UiType uiType, Transform parent)
        {
            _parent = parent;
            _uiType = uiType;
            _assetService = assetService;
        }

        public override void InstallBindings()
        {
            var prefab = _assetService.Load<GameObject>($"{_uiType}{DEFAULT_NAME}");

            var instance = Object.Instantiate(prefab, _parent, false);
            Container.InjectGameObject(instance);

            var component = instance.GetComponent<BaseUi>();

            if (component == null)
            {
                throw new Exception($"Prefab for {_uiType} does not contain a BaseUi component.");
            }

            Container.BindInterfacesTo(component.GetType())
                .FromInstance(component)
                .AsSingle();

            Container.Bind<BaseUi>()
                .FromInstance(component)
                .AsSingle();
        }
    }
}