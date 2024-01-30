using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Ship;
using UnityEngine;
using Zenject;

public class SkirmishServiceInstaller : MonoInstaller
{
    [SerializeField] private Camera mainCamera;
    
    public override void InstallBindings()
    {
        Container.BindInstance(mainCamera);
        
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

        Container
            .BindInterfacesAndSelfTo<CameraService>()
            .AsSingle()
            .NonLazy();

        Container
            .BindInterfacesAndSelfTo<EnemyService>()
            .AsCached()
            .NonLazy();
    }
}