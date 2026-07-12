using System.Linq;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Mvc;
using Utilities.ScriptUtils.Layer;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Radar
{
    public interface IRadarComponent : IComponent, IUnitComponent
    {
        ObservableList<IEntity> Enemies { get; }
        void SetPosition(Vector3 position);
    }
    public class RadarComponent : MonoComponent<RadarModel>, IInitializable, IFixedTickable, IRadarComponent
    {
        private const int HIT_LIMIT = 5;
        private const float OFFSET_DISTANCE = 100f;

        private IEntityLocator _entityLocator;
        private ITimer _timer;
        private Vector3 _offset;
        private Vector3 _halfExtents;

        private int _hitAmount;

        private RaycastHit[] _raycastHits = new RaycastHit[HIT_LIMIT];
        private IUnitMediator _unitMediator;
        private Vector3 _position;
        public ObservableList<IEntity> Enemies => Model.Enemies;
        private Vector3 CenterCast => _position - _offset;
        [Inject]
        private void Construct(IModel model, IEntityLocator entityLocator)
        {
            SetModel(model.GetModel<RadarModel>());
            _entityLocator = entityLocator;
        }

        public void Initialize()
        {
            _offset = Vector3.up * OFFSET_DISTANCE;
            _halfExtents = Vector3.one * Model.Range;
            _timer = TimerFactory.ConstructTimer(Model.Delay);
            _timer.StartTimer();

            int layer = Model.LayerMask.ToSingleLayer();
            gameObject.layer = layer;
            foreach (Transform child in gameObject.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = layer;
            }
        }

        public void SetPosition(Vector3 position)
        {
            _position = position;
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
                                if (_unitMediator != null)
                                {
                                    _unitMediator.HandleNewEnemy(entity);
                                }
                            }
                        }
                    }

                    //Model.AddHit(_raycastHits.Take(_hitAmount).ToArray());
                }
                _timer.StartTimer();
            }
        }

        public void SetMediator(IUnitMediator unitMediator)
        {
            _unitMediator = unitMediator;
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(CenterCast, _halfExtents * 2f);
            }
#endif
        }
    }
}
