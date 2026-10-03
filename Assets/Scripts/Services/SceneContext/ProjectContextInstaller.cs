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
using UnityEngine;
using UnityEngine.Audio;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class ProjectContextInstaller : MonoInstaller
    {
        [SerializeField] private CoroutineService coroutineService;
        [SerializeField] private AudioMixer audioMixer;

        public override void InstallBindings()
        {
            Container
                .Bind<ICoroutineService>()
                .To<CoroutineService>()
                .FromInstance(coroutineService)
                .AsSingle();
            
            
            var assetService = new AddressableAssetService();
            Container.BindInterfacesAndSelfTo<AddressableAssetService>().FromInstance(assetService).AsSingle();
        
            ModelDependencyBuilder
                .ConstructBuilder(Container)
                .BindFromNewScriptable<GameData>(assetService); //todo: change it

            Container.BindModel<SceneData>(assetService);

            Container.Bind<TimerPoolService>().AsSingle();
            Container.Bind<AudioMixer>().FromInstance(audioMixer).AsSingle();

            Container
                .BindInterfacesNonLazyExt<TimerPoolTick>()
                .BindInterfacesExt<GameController>()
                .BindInterfacesExt<SceneService>();
            Container
                .BindInterfacesTo<JsonSettingsRepository>()
                .AsSingle()
                .WithArguments(Application.persistentDataPath);
            Container
                // Settings appliers run in this binding order: display, quality, audio, then input bindings.
                .BindInterfacesExt<DisplaySettingsApplier>()
                .BindInterfacesExt<GraphicsSettingsApplier>()
                .BindInterfacesExt<AudioSettingsApplier>()
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
