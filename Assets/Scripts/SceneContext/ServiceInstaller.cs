using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Ship;
using Zenject;

public class ServiceInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container
            .BindInterfacesAndSelfTo<NavigationService>()
            .AsSingle()
            .NonLazy();
            
        Container
            .BindInterfacesAndSelfTo<InputService>()
            .AsSingle()
            .NonLazy();

        Container
            .BindInterfacesAndSelfTo<ShipService>()
            .AsSingle()
            .NonLazy();
    }
}