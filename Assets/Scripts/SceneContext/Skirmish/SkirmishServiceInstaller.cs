using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleService;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.Services.EconomyMediator;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ship;
using EmpireAtWar.Ui.Base;
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
            // .BindInterfaces<EnemyService>()
            // .BindInterfacesExt<PlayerService>()
            .BindInterfacesExt<SelectionService>()
            .BindInterfacesExt<ComponentHub>()
            .BindInterfacesExt<BattleService>()
            .BindInterfacesExt<EconomyMediator>();

    }
}