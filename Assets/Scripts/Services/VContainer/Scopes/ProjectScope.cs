using System;
using System.Threading;
using EmpireAtWar.Entities.Fade;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Graphics;
using EmpireAtWar.Services.IdGeneration;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Ui.Base;
using Utilities.ScriptUtils.Time;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Audio;
using global::VContainer;
using global::VContainer.Unity;
using CoroutineHost = EmpireAtWar.Services.CoroutineService.CoroutineService;
using SceneLoader = EmpireAtWar.Services.SceneService.SceneService;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class ProjectScope : AssetLifetimeScope
    {
        private static readonly ProfilerMarker CONFIGURE_MARKER = new ProfilerMarker("ProjectScope.Configure");

        [SerializeField] private CoroutineHost coroutineService;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private MapGenerationSettings mapGenerationSettings;
        [SerializeField] private HardPointHealthSettings hardPointHealthSettings;

        private AddressableHandleLoader _assetLoader;

        public GameData GameData { get; private set; }
        public IAssetHandleLoader AssetLoader => _assetLoader;
        public BattleAssetPreloader BattlePreloader { get; private set; }

        protected override async void Awake()
        {
            autoRun = false;
            base.Awake();
            CancellationToken cancellationToken = destroyCancellationToken;
            _assetLoader = new AddressableHandleLoader();
            CachedAssetService assets = new CachedAssetService(_assetLoader);
            OwnAssets(assets);
            try
            {
                await _assetLoader.InitializeAsync(cancellationToken);
                BattlePreloader = new BattleAssetPreloader(_assetLoader);
                assets.BeginPreload<GameData>(nameof(GameData));
                assets.BeginPreload<SceneData>(nameof(SceneData));
                assets.BeginPreload<MusicAudioData>(nameof(MusicAudioData));
                assets.BeginPreload<GameObject>("MusicSource");
                BeginMainMenuPreload(assets);
                await assets.CompletePreloadsAsync(cancellationToken);
                GameData = Object.Instantiate(assets.Load<GameData>(nameof(GameData)));
                SceneType scene = assets.Load<SceneData>(nameof(SceneData)).GetCurrentScene();
                if (scene == SceneType.Battle)
                    await BattlePreloader.PrepareAsync(GameData.Battle, GameData.PendingBattleSave != null, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                Build();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch
            {
                if (BattlePreloader != null) BattlePreloader.Dispose();
                assets.Dispose();
                _assetLoader.Dispose();
                throw;
            }
        }

        protected override void Configure(IContainerBuilder builder)
        {
            using var marker = CONFIGURE_MARKER.Auto();
            builder.RegisterInstance(Assets);
            builder.RegisterInstance(hardPointHealthSettings.CreateBudget());
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

        private static void BeginMainMenuPreload(CachedAssetService assets)
        {
            assets.BeginPreload<FactionCatalog>(nameof(FactionCatalog));
            assets.BeginPreload<TooltipSettings>(nameof(TooltipSettings));
            assets.BeginPreload<TooltipIconData>(nameof(TooltipIconData));
            assets.BeginPreload<GameObject>(nameof(UiService));
            assets.BeginPreload<GameObject>(nameof(MainMenuUi));
            assets.BeginPreload<GameObject>(nameof(FadeUi));
            assets.BeginPreload<GameObject>(nameof(TooltipUi));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (BattlePreloader != null) BattlePreloader.Dispose();
            if (_assetLoader != null) _assetLoader.Dispose();
            if (Application.isPlaying) Object.Destroy(GameData);
            else Object.DestroyImmediate(GameData);
        }
    }
}
