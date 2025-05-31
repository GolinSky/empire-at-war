using System.Linq;
using EmpireAtWar.Components.Movement;
using EmpireAtWar.Entities.BaseEntity;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Radar
{
    public class RadarComponent : BaseComponent<RadarModel>, IFixedTickable
    {
        private const int HIT_LIMIT = 5;
        private const float OFFSET_DISTANCE = 100f;
        
        private readonly IEntityLocator _entityLocator;
        private readonly IDefaultMoveModelObserver _moveModel;
        private readonly ITimer _timer;
        private readonly Vector3 _offset;
        private readonly Vector3 _halfExtents;

        private int _hitAmount;

        private RaycastHit[] _raycastHits = new RaycastHit[HIT_LIMIT];
        private Vector3 CenterCast => _moveModel.CurrentPosition - _offset;
        public RadarComponent(IModel model, IEntityLocator entityLocator) : base(model)
        {
            _entityLocator = entityLocator;
            _offset = Vector3.up * OFFSET_DISTANCE;
            _halfExtents = Vector3.one * Model.Range;
            _timer = TimerFactory.ConstructTimer(Model.Delay);
            _timer.StartTimer();
            _moveModel = model.GetModelObserver<IDefaultMoveModelObserver>();
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
                    Model.EnemyLayerMask);// todo: use player type instead maybe
               

                if (_raycastHits != null && _raycastHits.Length != 0 && _hitAmount != 0)
                {
                    _raycastHits = _raycastHits.Take(_hitAmount).ToArray();
                    
                    for (var i = 0; i < _raycastHits.Length; i++)
                    {
                        if (_entityLocator.TryGetEntity(_raycastHits[i], out IEntity entity))
                        {
                            if (!Model.Enemies.Contains(entity))
                            {
                                Model.Enemies.Add(entity);
                            }
                        }
                    }
                    
                    //Model.AddHit(_raycastHits.Take(_hitAmount).ToArray());
                }
                _timer.StartTimer();
            }
        }
    }
}