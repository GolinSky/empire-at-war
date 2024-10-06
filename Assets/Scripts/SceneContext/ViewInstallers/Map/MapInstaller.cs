using EmpireAtWar.Controllers.Map;
using EmpireAtWar.Models.Map;
using Zenject;

namespace EmpireAtWar.SceneContext.ViewInstallers.Map
{
    public class MapInstaller:StaticViewInstaller<MapController, MapModel>
    {
        [Inject]
        private Zenject.SceneContext SceneContext { get; }
        
        protected override void BindModel()
        {
            base.BindModel();
            SceneContext.Container.Bind<IMapModelObserver>().FromMethod((() => Container.Resolve<IMapModelObserver>()));
        }
    }
}