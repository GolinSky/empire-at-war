using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleService;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.SceneContext.Skirmish
{
    public class SkirmishServiceInstaller : MonoInstaller
    {
        [Inject] private IRepository Repository { get; }

        public override void InstallBindings()
        {
            Container.BindScriptableObject<CameraData>(Repository);
            Container
                .BindInterfacesAndSelfTo<CameraService>()
                .FromComponentInHierarchy()
                .AsSingle();

            Container
                .BindInterfacesExt<InputService>()
                .BindInterfacesExt<ShipService>()
                .BindInterfacesExt<SelectionService>()
                .BindInterfacesExt<BattleService>();
        }
    }
}
