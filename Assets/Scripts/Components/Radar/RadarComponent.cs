using System;
using System.Collections.Generic;
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
        private const int INITIAL_HIT_LIMIT = 64;
        private const int MAX_HIT_LIMIT = 2048;

        private IEntityLocator _entityLocator;
        private ITimer _timer;
        private Vector3 _halfExtents;

        private Collider[] _overlapHits = new Collider[INITIAL_HIT_LIMIT];
        private readonly HashSet<IEntity> _detectedEnemies = new HashSet<IEntity>();
        private IUnitMediator _unitMediator;
        private Vector3 _position;
        public ObservableList<IEntity> Enemies => Model.Enemies;
        [Inject]
        private void Construct(RadarModel model, IEntityLocator entityLocator)
        {
            SetModel(model);
            _entityLocator = entityLocator;
        }

        public void Initialize()
        {
            _halfExtents = new Vector3(Model.Range, Model.Distance * 0.5f, Model.Range);
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
                int hitAmount = GetOverlapHits();
                _detectedEnemies.Clear();
                for (int i = 0; i < hitAmount; i++)
                {
                    if (_entityLocator.TryGetEntity(_overlapHits[i], out IEntity entity) &&
                        !entity.HealthModel.IsDestroyed)
                    {
                        _detectedEnemies.Add(entity);
                    }
                }

                for (int i = Model.Enemies.Count - 1; i >= 0; i--)
                {
                    if (!_detectedEnemies.Contains(Model.Enemies[i]))
                    {
                        Model.Enemies.RemoveAt(i);
                    }
                }

                foreach (IEntity entity in _detectedEnemies)
                {
                    if (Model.Enemies.Contains(entity))
                    {
                        continue;
                    }

                    Model.Enemies.Add(entity);
                    _unitMediator?.HandleNewEnemy(entity);
                }

                _timer.StartTimer();
            }
        }

        private int GetOverlapHits()
        {
            while (true)
            {
                int hitAmount = Physics.OverlapBoxNonAlloc(
                    _position,
                    _halfExtents,
                    _overlapHits,
                    Quaternion.identity,
                    Model.EnemyLayerMask,
                    QueryTriggerInteraction.Collide);
                if (hitAmount < _overlapHits.Length || _overlapHits.Length >= MAX_HIT_LIMIT)
                {
                    return hitAmount;
                }

                int newSize = Math.Min(_overlapHits.Length * 2, MAX_HIT_LIMIT);
                Array.Resize(ref _overlapHits, newSize);
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
                Gizmos.DrawWireCube(_position, _halfExtents * 2f);
            }
#endif
        }
    }
}
