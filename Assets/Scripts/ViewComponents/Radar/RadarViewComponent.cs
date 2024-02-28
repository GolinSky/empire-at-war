using EmpireAtWar.Models.Radar;
using Utilities.ScriptUtils.Layer;
using WorkShop.LightWeightFramework.ViewComponents;

namespace EmpireAtWar.ViewComponents.Radar
{
    public class RadarViewComponent:ViewComponent
    {
        private IRadarModelObserver radarModelObserver;
        protected override void OnInit()
        {
            radarModelObserver = ModelObserver.GetModelObserver<IRadarModelObserver>();
            View.gameObject.layer = radarModelObserver.LayerMask.ToSingleLayer();
        }
  
        protected override void OnRelease()
        {
            
        }
    }
}