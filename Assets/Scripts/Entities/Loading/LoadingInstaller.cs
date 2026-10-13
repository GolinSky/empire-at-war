using EmpireAtWar.Entities.Loading;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext.ViewInstallers.Loading
{
    public sealed class LoadingInstaller : MonoInstaller
    {
        [SerializeField] private LoadingUi view;

        public override void InstallBindings()
        {
            Container.BindInstance(view);
            Container.BindInterfacesTo<LoadingUiController>().AsSingle();
            Container.BindInterfacesTo<SceneActivationFlow>().AsSingle();
        }
    }
}
