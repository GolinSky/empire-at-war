using EmpireAtWar.Models.Radar;
using Utilities.ScriptUtils.Layer;
using LightWeightFramework.Components.ViewComponents;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Radar
{
    public class RadarViewComponent:ViewComponent
    {
        private IRadarModelObserver radarModelObserver;
        protected override void OnInit()
        {
            radarModelObserver = ModelObserver.GetModelObserver<IRadarModelObserver>();
            int layer = radarModelObserver.LayerMask.ToSingleLayer();
            View.gameObject.layer = layer;
            Transform[] children = View.gameObject.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = layer;
            }
        }
  
        protected override void OnRelease()
        {
            
        }
    }
}