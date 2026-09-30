using EmpireAtWar.Extentions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Ui.Base
{
    public class UiInstaller: Installer
    {
        private const string DEFAULT_NAME = "Ui";
        private IAssetService _assetService;
        private UiType _uiType;
        private Transform _parent;
        private GameObjectContext _context;
        
        [Inject]
        public void Constructor(IAssetService assetService, UiType uiType, Transform parent, GameObjectContext context)
        {
            _parent = parent;
            _uiType = uiType;
            _assetService = assetService;
            _context = context;
        }

        public override void InstallBindings()
        {
            var prefab = _assetService.LoadComponent<BaseUi>($"{_uiType}{DEFAULT_NAME}");

            var component = Container.InstantiatePrefabForInstall(prefab, _parent, _context);

            Container.BindInterfacesTo(component.GetType())
                .FromInstance(component)
                .AsSingle();

            Container.Bind<BaseUi>()
                .FromInstance(component)
                .AsSingle();
        }
    }
}
