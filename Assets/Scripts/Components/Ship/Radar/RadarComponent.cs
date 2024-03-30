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
        private const int HitLimit = 5;
        private const float OffsetDistance = 100f;
        
        private readonly ITimer timer;
        private readonly ISimpleMoveModelObserver moveModel;
        private readonly Vector3 offset;
        private readonly Vector3 halfExtents;

        private int hitAmount;

        private RaycastHit[] raycastHits = new RaycastHit[HitLimit];

        private Vector3 CenterCast => moveModel.CurrentPosition - offset;
        public RadarComponent(IModel model) : base(model)
        {
            offset = Vector3.up * OffsetDistance;
            halfExtents = Vector3.one * Model.Range;
            timer = TimerFactory.ConstructTimer(Model.Delay);
            timer.StartTimer();
            moveModel = model.GetModelObserver<ISimpleMoveModelObserver>();
        }

        public void FixedTick()
        {
            if (timer.IsComplete)
            {
                hitAmount = Physics.BoxCastNonAlloc(
                    CenterCast,
                    halfExtents,
                    Vector3.up,
                    raycastHits,
                    Quaternion.identity,
                    Model.Distance + offset.y, //todo : fix this
                    Model.EnemyLayerMask);
               

                if (raycastHits != null && raycastHits.Length != 0 && hitAmount != 0)
                {
                    raycastHits = raycastHits.Take(hitAmount).ToArray();
                    Model.AddHit(raycastHits.Take(hitAmount).ToArray());
                }
                timer.StartTimer();
            }
        }
    }
}