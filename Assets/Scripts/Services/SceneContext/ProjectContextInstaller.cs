using EmpireAtWar.Entities.Game;
using EmpireAtWar.Extentions;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.CoroutineService;
using EmpireAtWar.Services.Graphics;
using EmpireAtWar.Services.IdGeneration;
using EmpireAtWar.Services.Input;
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
        
        private IAssetService _assetService;
    
        public override void InstallBindings()
        {
            Container
                .Bind<ICoroutineService>()
                .To<CoroutineService>()
                .FromInstance(coroutineService)
                .AsSingle();
            
            
            Container.BindInterfacesExt<AddressableAssetService>();
        
            _assetService = Container.Resolve<IAssetService>();
        
            ModelDependencyBuilder
                .ConstructBuilder(Container)
                .BindFromNewScriptable<GameData>(_assetService); //todo: change it

            Container.BindModel<SceneData>(_assetService);

            Container.Bind<TimerPoolService>().AsSingle();

            Container
                .BindInterfacesNonLazyExt<TimerPoolTick>()
                .BindInterfacesExt<GameController>()
                .BindInterfacesExt<SceneService>()
                .BindInterfacesExt<JsonSettingsRepository>()
                // Settings appliers run in this binding order: display, quality, then input bindings.
                .BindInterfacesExt<DisplaySettingsApplier>()
                .BindInterfacesExt<GraphicsSettingsApplier>()
                .BindInterfacesExt<SettingsService>()
                .BindInterfacesExt<AudioService>()
                .BindInterfacesExt<MusicService>()
                .BindInterfacesExt<UniqueIdGenerator>()
                .BindInterfacesExt<InputActionsProvider>()
                .BindInterfacesExt<InputBindingService>()
                .BindInterfacesExt<CancelInput>();
        }
    }
}
