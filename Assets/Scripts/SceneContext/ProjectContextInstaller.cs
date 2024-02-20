using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Game;
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
            .BindModel<GameModel>();
        Container
            .BindService<GameController>();
        
     
        
        Container
            .BindService<SceneService>();
        
        Container
            .BindService<SettingsService>();
        
        Container
            .BindService<AudioService>();
    }
    
   
}