using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Graphics;
using EmpireAtWar.Services.IdGeneration;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.Timing;
using Utilities.ScriptUtils.Time;
using UnityEngine;
using UnityEngine.Audio;
using global::VContainer;
using global::VContainer.Unity;
using CoroutineHost = EmpireAtWar.Services.CoroutineService.CoroutineService;
using SceneLoader = EmpireAtWar.Services.SceneService.SceneService;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class ProjectScope : AssetLifetimeScope
    {
        [SerializeField] private CoroutineHost coroutineService;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private MapGenerationSettings mapGenerationSettings;

        public GameData GameData { get; private set; }
        public IAssetHandleLoader AssetLoader { get; private set; }

        protected override void Configure(IContainerBuilder builder)
        {
            AssetLoader = new AddressableHandleLoader();
            OwnAssets(new CachedAssetService(AssetLoader));
            GameData = Object.Instantiate(Assets.Load<GameData>(nameof(GameData)));
            builder.RegisterInstance(Assets);
            builder.RegisterInstance(GameData).AsImplementedInterfaces().AsSelf();
            AssetRegistrations.RegisterShared<SceneData>(builder, Assets);
            builder.RegisterComponent(coroutineService).AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(audioMixer);
            builder.Register<TimerPoolService>(Lifetime.Singleton);
            builder.Register<TimerPoolTick>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(new BattleSaveValidator(Assets, mapGenerationSettings));
            builder.Register<GameController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SceneLoader>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<JsonSettingsRepository>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
                .WithParameter(Application.persistentDataPath);
            builder.Register<JsonBattleSaveRepository>(Lifetime.Singleton).AsImplementedInterfaces()
                .WithParameter(Application.persistentDataPath);
            builder.Register<DisplaySettingsApplier>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<GraphicsSettingsApplier>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<AudioSettingsApplier>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SettingsService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<AudioService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<MusicService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UniqueIdGenerator>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<InputActionsProvider>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<InputBindingService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CancelInput>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterLifecycle();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Application.isPlaying) Object.Destroy(GameData);
            else Object.DestroyImmediate(GameData);
        }
    }
}
