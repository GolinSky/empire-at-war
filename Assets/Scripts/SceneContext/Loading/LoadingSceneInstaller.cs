using EmpireAtWar.Controllers.Loading;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Loading;
using EmpireAtWar.Views.Loading;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext.Loading
{
    public class LoadingSceneInstaller : MonoInstaller
    {
        [SerializeField] private LoadingView loadingView;
        
        public override void InstallBindings()
        {
            Container.BindEntityNoCommand<LoadingController, LoadingView, LoadingModel>(
                loadingView);
        }
    }
}