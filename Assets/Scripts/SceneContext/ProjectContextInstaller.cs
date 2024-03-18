using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Audio;
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
        Container.BindInterfacesAndSelfTo<AddressableRepository>()
            .AsSingle();

        repository = Container.Resolve<IRepository>();
        
        ModelDependencyBuilder
            .ConstructBuilder(Container)
            .BindFromNewScriptable<GameModel>(repository);
        
        Container
            .BindInterfaces<TimerPoolWrapperService>()
            .BindInterfaces<GameController>()
            .BindInterfaces<SceneService>()
            .BindInterfaces<SettingsService>()
            .BindInterfaces<AudioService>();
    }
}