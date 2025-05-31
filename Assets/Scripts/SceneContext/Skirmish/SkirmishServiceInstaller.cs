using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleService;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Ship;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext.Skirmish
{
    public class SkirmishServiceInstaller : MonoInstaller
    {
        [SerializeField] private Camera mainCamera;

        public override void InstallBindings()
        {
            Container.BindInstance(mainCamera);
            Container
                .BindInterfacesExt<InputService>()
                .BindInterfacesExt<ShipService>()
                .BindInterfacesExt<CameraService>()
                .BindInterfacesExt<SelectionService>()
                .BindInterfacesExt<BattleService>();

        }
    }
}