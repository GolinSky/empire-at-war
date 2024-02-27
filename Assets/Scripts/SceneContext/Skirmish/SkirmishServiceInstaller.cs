using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Reinforcement;
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
            .BindService<NavigationService>()
            .BindService<InputService>()
            .BindService<ShipService>()
            .BindService<CameraService>()
            .BindService<EnemyService>()
            .BindService<PlayerService>()
            .BindService<SelectionService>()
            .BindService<ReinforcementService>()
            .BindService<ComponentHub>();
    }
}