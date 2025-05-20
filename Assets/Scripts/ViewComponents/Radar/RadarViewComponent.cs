using System;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Movement;
using Utilities.ScriptUtils.Layer;
using LightWeightFramework.Components.ViewComponents;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Radar
{
    public class RadarViewComponent:ViewComponent
    {
        private IRadarModelObserver _radarModelObserver;
        private Vector3 _offset;
        private IDefaultMoveModelObserver _moveModel;
        private Vector3 _halfExtents;
        private Vector3 CenterCast => _moveModel.CurrentPosition - _offset;

        protected override void OnInit()
        {
            _moveModel = ModelObserver.GetModelObserver<IDefaultMoveModelObserver>();

            _radarModelObserver = ModelObserver.GetModelObserver<IRadarModelObserver>();
            int layer = _radarModelObserver.LayerMask.ToSingleLayer();
            View.gameObject.layer = layer;
            Transform[] children = View.gameObject.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = layer;
            }
            _offset = Vector3.up * 100f;
            _halfExtents = Vector3.one * _radarModelObserver.Range;

        }
  
        protected override void OnRelease()
        {
            
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(CenterCast, _halfExtents*2f);
            }
#endif
      
        }
    }
}