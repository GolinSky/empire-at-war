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

        private RaycastHit[] raycastHits;

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
                raycastHits = Physics.BoxCastAll(
                    CenterCast,
                    Vector3.one * Model.Range,
                    Vector3.up,
                    Quaternion.identity,
                    Model.Distance + offset.y,
                    Model.EnemyLayerMask);

                if (raycastHits != null && raycastHits.Length != 0)
                {
                    Model.AddHit(raycastHits);
                }
                timer.StartTimer();
            }
        }
    }
}