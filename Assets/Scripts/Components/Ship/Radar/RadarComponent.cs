using System.Linq;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Ship.Radar
{
    public class RadarComponent : BaseComponent<RadarModel>, IFixedTickable
    {
        private const float OffsetDistance = 100f;
        
        private readonly ITimer timer;
        private readonly ISimpleMoveModelObserver simpleMoveModelObserver;
        private readonly Vector3 offset;

        private int hitAmount;

        private RaycastHit[] raycastHits = new RaycastHit[5];

        private Vector3 CenterCast => simpleMoveModelObserver.CurrentPosition - offset;
        public RadarComponent(IModel model) : base(model)
        {
            offset = Vector3.up * OffsetDistance;
            timer = TimerFactory.ConstructTimer(Model.Delay);
            timer.StartTimer();
            simpleMoveModelObserver = model.GetModelObserver<ISimpleMoveModelObserver>();
        }

        public void FixedTick()
        {
            if (timer.IsComplete)
            {
                hitAmount = Physics.BoxCastNonAlloc(
                    CenterCast,
                    Vector3.one * Model.Range,
                    Vector3.up,
                    raycastHits,
                    Quaternion.identity,
                    Model.Distance + offset.y,
                    Model.EnemyLayerMask);
               

                if (raycastHits != null && raycastHits.Length != 0 && hitAmount != 0)
                {
                    Model.AddHit(raycastHits.Take(hitAmount).ToArray());
                }
                timer.StartTimer();
            }
        }
    }
}