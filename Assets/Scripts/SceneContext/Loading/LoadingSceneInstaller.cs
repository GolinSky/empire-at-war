using EmpireAtWar.Controllers.Loading;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Loading;
using EmpireAtWar.Views.Loading;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext.Loading
{
    public class LoadingSceneInstaller : MonoInstaller
    {
        [SerializeField] private LoadingView loadingView;
        
        [Inject]
        private IRepository Repository { get; }
        
        public override void InstallBindings()
        {
            Container
                .BindModel<LoadingModel>(Repository)
                .BindInterfaces<LoadingController>()
                .BindViewFromInstance(loadingView);
        }
    }
}