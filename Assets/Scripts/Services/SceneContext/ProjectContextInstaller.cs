using EmpireAtWar.Entities.Game;
using EmpireAtWar.Extentions;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.CoroutineService;
using EmpireAtWar.Services.IdGeneration;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.Timing;
using Utilities.ScriptUtils.Time;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class ProjectContextInstaller : MonoInstaller
    {
        [SerializeField] private CoroutineService coroutineService;
        
        private IAssetService _repository;
    
        public override void InstallBindings()
        {
            Container
                .Bind<ICoroutineService>()
                .To<CoroutineService>()
                .FromInstance(coroutineService)
                .AsSingle();
            
            
            Container.BindInterfacesExt<AddressableAssetService>();
        
            _repository = Container.Resolve<IAssetService>();
        
            ModelDependencyBuilder
                .ConstructBuilder(Container)
                .BindFromNewScriptable<GameData>(_repository); //todo: change it

            Container.BindModel<SceneData>(_repository);

            Container.Bind<TimerPoolService>().AsSingle();

            Container
                .BindInterfacesNonLazyExt<TimerPoolTick>()
                .BindInterfacesExt<GameController>()
                .BindInterfacesExt<SceneService>()
                .BindInterfacesExt<SettingsService>()
                .BindInterfacesExt<AudioService>()
                .BindInterfacesExt<UniqueIdGenerator>();
        }
    }
}
