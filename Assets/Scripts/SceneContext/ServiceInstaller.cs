using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.NavigationService;
using Zenject;

public class ServiceInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container
            .BindInterfacesAndSelfTo<NavigationService>()
            .AsSingle();
            
        Container
            .BindInterfacesAndSelfTo<InputService>()
            .AsSingle();
    }
}