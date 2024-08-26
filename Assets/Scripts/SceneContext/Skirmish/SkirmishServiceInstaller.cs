using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleService;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.Services.EconomyMediator;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Player;
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
            .BindInterfaces<NavigationService>()
            .BindInterfaces<InputService>()
            .BindInterfaces<ShipService>()
            .BindInterfaces<CameraService>()
            .BindInterfaces<EnemyService>()
            .BindInterfaces<PlayerService>()
            .BindInterfaces<SelectionService>()
            .BindInterfaces<ComponentHub>()
            .BindInterfaces<BattleService>()
            .BindInterfaces<EconomyMediator>();
    }
}