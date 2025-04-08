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
        private const int HIT_LIMIT = 5;
        private const float OFFSET_DISTANCE = 100f;
        
        private readonly ITimer _timer;
        private readonly ISimpleMoveModelObserver _moveModel;
        private readonly Vector3 _offset;
        private readonly Vector3 _halfExtents;

        private int _hitAmount;

        private RaycastHit[] _raycastHits = new RaycastHit[HIT_LIMIT];

        private Vector3 CenterCast => _moveModel.CurrentPosition - _offset;
        public RadarComponent(IModel model) : base(model)
        {
            _offset = Vector3.up * OFFSET_DISTANCE;
            _halfExtents = Vector3.one * Model.Range;
            _timer = TimerFactory.ConstructTimer(Model.Delay);
            _timer.StartTimer();
            _moveModel = model.GetModelObserver<ISimpleMoveModelObserver>();
        }

        public void FixedTick()
        {
            if (_timer.IsComplete)
            {
                _hitAmount = Physics.BoxCastNonAlloc(
                    CenterCast,
                    _halfExtents,
                    Vector3.up,
                    _raycastHits,
                    Quaternion.identity,
                    Model.Distance + _offset.y, //todo : fix this
                    Model.EnemyLayerMask);
               

                if (_raycastHits != null && _raycastHits.Length != 0 && _hitAmount != 0)
                {
                    _raycastHits = _raycastHits.Take(_hitAmount).ToArray();
                    Model.AddHit(_raycastHits.Take(_hitAmount).ToArray());
                }
                _timer.StartTimer();
            }
        }
    }
}