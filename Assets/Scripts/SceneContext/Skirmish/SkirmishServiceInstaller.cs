using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleService;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ship;
using UnityEngine;
using Zenject;

public class SkirmishServiceInstaller : MonoInstaller
{
    [SerializeField] private Camera mainCamera;

    public override void InstallBindings()
    {
        Container.BindInstance(mainCamera);
        Container
            .BindInterfacesExt<NavigationService>()
            .BindInterfacesExt<InputService>()
            .BindInterfacesExt<ShipService>()
            .BindInterfacesExt<CameraService>()
            .BindInterfacesExt<SelectionService>()
            .BindInterfacesExt<ComponentHub>()
            .BindInterfacesExt<BattleService>();

    }
}