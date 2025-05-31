using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Extentions;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.IdGeneration;
using EmpireAtWar.Services.Initialiaze;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.TimerPoolWrapperService;
using LightWeightFramework.Components.Repository;
using Zenject;

public class ProjectContextInstaller : MonoInstaller
{
    private IRepository _repository;
    public override void InstallBindings()
    {
        Container.Bind<IInitializable>().To<LateInitializableService>().AsSingle();
        Container.BindExecutionOrder<LateInitializableService>(10); // Set a higher order to execute later


        Container.BindInterfacesExt<AddressableRepository>();
        
        _repository = Container.Resolve<IRepository>();
        
        ModelDependencyBuilder
            .ConstructBuilder(Container)
            .BindFromNewScriptable<GameModel>(_repository);

        Container.BindModel<SceneModel>(_repository);

        Container
            .BindInterfacesExt<TimerPoolWrapperService>()
            .BindInterfacesExt<GameController>()
            .BindInterfacesExt<SceneService>()
            .BindInterfacesExt<SettingsService>()
            .BindInterfacesExt<AudioService>()
            .BindInterfacesExt<UniqueIdGenerator>();
    }
}