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
            var prefab = _assetService.LoadComponent<BaseUi>($"{_uiType}{DEFAULT_NAME}");

            var component = Object.Instantiate(prefab, _parent, false);
            Container.InjectGameObject(component.gameObject);

            Container.BindInterfacesTo(component.GetType())
                .FromInstance(component)
                .AsSingle();

            Container.Bind<BaseUi>()
                .FromInstance(component)
                .AsSingle();
        }
    }
}