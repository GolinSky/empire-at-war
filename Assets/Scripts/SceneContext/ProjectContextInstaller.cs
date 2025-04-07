using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Initialiaze;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.TimerPoolWrapperService;
using LightWeightFramework.Components.Repository;
using Zenject;

public class ProjectContextInstaller : MonoInstaller
{
    private IRepository repository;
    public override void InstallBindings()
    {
        Container.Bind<IInitializable>().To<LateInitializableService>().AsSingle();
        Container.BindExecutionOrder<LateInitializableService>(10); // Set a higher order to execute later


        Container.BindInterfacesExt<AddressableRepository>();
        
        repository = Container.Resolve<IRepository>();
        
        ModelDependencyBuilder
            .ConstructBuilder(Container)
            .BindFromNewScriptable<GameModel>(repository);

        Container.BindModel<SceneModel>(repository);
        
        Container
            .BindInterfacesExt<TimerPoolWrapperService>()
            .BindInterfacesExt<GameController>()
            .BindInterfacesExt<SceneService>()
            .BindInterfacesExt<SettingsService>()
            .BindInterfacesExt<AudioService>();
    }
}