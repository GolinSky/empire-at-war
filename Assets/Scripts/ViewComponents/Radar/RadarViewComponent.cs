using System;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using Utilities.ScriptUtils.Layer;
using LightWeightFramework.Components.ViewComponents;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Radar
{
    public class RadarViewComponent:ViewComponent
    {
        private IRadarModelObserver radarModelObserver;
        private Vector3 offset;
        private ISimpleMoveModelObserver moveModel;
        private Vector3 halfExtents;
        private Vector3 CenterCast => moveModel.CurrentPosition - offset;

        protected override void OnInit()
        {
            moveModel = ModelObserver.GetModelObserver<ISimpleMoveModelObserver>();

            radarModelObserver = ModelObserver.GetModelObserver<IRadarModelObserver>();
            int layer = radarModelObserver.LayerMask.ToSingleLayer();
            View.gameObject.layer = layer;
            Transform[] children = View.gameObject.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = layer;
            }
            offset = Vector3.up * 100f;
            halfExtents = Vector3.one * radarModelObserver.Range;

        }
  
        protected override void OnRelease()
        {
            
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(CenterCast, halfExtents*2f);
        }
    }
}