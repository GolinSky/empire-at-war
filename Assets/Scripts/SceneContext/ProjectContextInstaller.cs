using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.TimerPoolWrapperService;
using Zenject;

public class ProjectContextInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<AddressableRepository>()
            .AsSingle();
        
        Container
            .BindService<TimerPoolWrapperService>();
        
        Container
            .BindService<SceneService>();

        Container.BindInstance(new SkirmishGameData()) // fix this 
            .AsSingle();

        Container
            .BindService<SettingsService>();
        
        Container
            .BindService<AudioService>();
    }
}