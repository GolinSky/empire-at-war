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
        private readonly ITimer timer;
        private readonly IMoveModelObserver moveModelObserver;
        private readonly Vector3 offset;

        private RaycastHit[] raycastHits;

        private Vector3 CenterCast => moveModelObserver.CurrentPosition - offset;
        public RadarComponent(IModel model) : base(model)
        {
            offset = Vector3.up * 100;
            timer = TimerFactory.ConstructTimer(Model.Delay);
            timer.StartTimer();
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
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
                    // string names = "RaycastHits: ";
                    // foreach (var raycastHit in raycastHits)
                    // {
                    //     names += $"{raycastHit.collider.name},  ";
                    // }
                    Model.AddHit(raycastHits);
                }
                timer.StartTimer();
            }
        }
    }
}