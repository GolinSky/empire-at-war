using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using LightWeightFramework.Model;
using UnityEngine;
using Utils.TimerService;
using Zenject;

namespace EmpireAtWar.Components.Ship.Radar
{
    public class RadarComponent : BaseComponent<RadarModel>, IFixedTickable
    {
        private readonly Vector3 offset;
        private readonly ITimer timer;
        private readonly IMoveModelObserver moveModelObserver;

        private bool isDetected;
        private RaycastHit raycastHit;

        private Vector3 CenterCast => moveModelObserver.Position - offset;
        public RadarComponent(IModel model) : base(model)
        {
            offset = Vector3.up * 50;
            timer = TimerFactory.ConstructTimer(Model.Delay);
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
        }


        public void FixedTick()
        {
            if (timer.IsComplete)
            {
                isDetected = Physics.BoxCast(
                    CenterCast,
                    Vector3.one*40,
                    Vector3.up,
                    out raycastHit,
                    Quaternion.identity,
                    Model.Distance,
                    Model.EnemyLayerMask);

                if (isDetected)
                {
                    Debug.Log($"raycastHit: {raycastHit.collider.name}");
                }
                timer.StartTimer();
            }
        }
    }
}