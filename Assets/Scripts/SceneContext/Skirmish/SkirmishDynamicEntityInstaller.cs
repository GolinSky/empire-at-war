using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.Ship;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class SkirmishDynamicEntityInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .BindFactory<ShipType, ShipView, ShipFacadeFactory>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<ShipInstaller>()
                .NonLazy();
        }
    }
}