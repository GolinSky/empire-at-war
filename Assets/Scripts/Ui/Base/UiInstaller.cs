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
            // var prefab = _repository.Load<GameObject>($"{_uiType}{DEFAULT_NAME}");
            //
            // var componentUi = prefab.GetComponent<BaseUi>();
            //
            // Container.BindInterfacesTo(componentUi.GetType())
            //     .FromComponentInNewPrefab(_repository.LoadComponent<BaseUi>($"{_uiType}{DEFAULT_NAME}"))
            //     .UnderTransform(_parent)
            //     .AsSingle();
            
            
            var prefab = _repository.Load<GameObject>($"{_uiType}{DEFAULT_NAME}");

            // Instantiate manually, do NOT let Zenject do the prefab loading
            var instance = Object.Instantiate(prefab, _parent, false);
            Container.InjectGameObject(instance);

            // Grab the BaseUi-derived component
            var component = instance.GetComponent<BaseUi>();

            if (component == null)
            {
                throw new Exception($"Prefab for {_uiType} does not contain a BaseUi component.");
            }

            // Bind dynamically to all interfaces, including IInitializable, etc.
            Container.BindInterfacesTo(component.GetType())
                .FromInstance(component)
                .AsSingle();

            // Optional: if you also want to return it directly
            Container.Bind<BaseUi>()
                .FromInstance(component)
                .AsSingle();

        }
    }
}