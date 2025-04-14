using System;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Ui.Base
{
    public class UiInstaller: Installer
    {
        private const string DEFAULT_NAME = "Ui";
        private IRepository _repository;
        private UiType _uiType;
        private Transform _parent;
        
        [Inject]
        public void Constructor(IRepository repository, UiType uiType, Transform parent)
        {
            _parent = parent;
            _uiType = uiType;
            _repository = repository;
        }

        public override void InstallBindings()
        {
            var prefab = _repository.Load<GameObject>($"{_uiType}{DEFAULT_NAME}");

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