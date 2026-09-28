using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Timing;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities.ScriptUtils.Time;
using EmpireAtWar.Services.Layer;
using Zenject;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Utils;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Cheats;

namespace EmpireAtWar.Components.Radar
{
    public interface IRadarComponent : IComponent
    {
        event Action<IReadOnlyList<RadarContact>> ContactsUpdated;
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
        private readonly List<RadarContact> _contacts = new List<RadarContact>();
        private ILayerService _layerService;
        private IRangeDebugObserver _rangeDebug;
        private ISelectionModelObserver _selection;
        private IPlayerRelations _relations;
        private Vector3 _position;
        private bool _isReleased;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private DebugRangeCircle _radarRangeCircle;
#endif
        public event Action<IReadOnlyList<RadarContact>> ContactsUpdated;
        public ObservableList<IEntity> Enemies => Model.Enemies;
        [Inject]
        private void Construct(RadarModel model, IEntityLocator entityLocator, ILayerService layerService,
            IRangeDebugObserver rangeDebug, ISelectionModelObserver selection, IPlayerRelations relations)
        {
            SetModel(model);
            _entityLocator = entityLocator;
            _layerService = layerService;
            _rangeDebug = rangeDebug;
            _selection = selection;
            _relations = relations;
        }

        public void Initialize()
        {
            _halfExtents = new Vector3(Model.Range, Model.Distance * 0.5f, Model.Range);
            _timer = TimerFactory.ConstructTimer(Model.Delay);
            _timer.StartTimer();

            _layerService.Apply(gameObject, LayerKey.Unit, true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _radarRangeCircle = new DebugRangeCircle("RadarRange", new Color(0.2f, 0.6f, 1f), _rangeDebug, _selection);
#endif
        }

        public void SetPosition(Vector3 position)
        {
            _position = position;
        }

        public void FixedTick()
        {
            if (_isReleased)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _radarRangeCircle.Draw(_position, Model.Range);
#endif
            if (_timer.IsComplete)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.RadarScan.Auto())
                {
#endif
                int hitAmount = GetOverlapHits();
                _detectedEnemies.Clear();
                _contacts.Clear();
                for (int i = 0; i < hitAmount; i++)
                {
                    Collider hit = _overlapHits[i];
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        continue;
                    }

                    if (_entityLocator.TryGetEntity(_overlapHits[i], out IEntity entity) &&
                        !entity.HealthModel.IsDestroyed)
                    {
                        if (_relations.IsHostile(Model.Owner, entity.Owner) && entity.HealthModel.HasUnits &&
                            (entity.GetFacade<IEntityTransformFacade>().Transform.position - _position).sqrMagnitude <=
                            Model.Range * Model.Range)
                        {
                            _detectedEnemies.Add(entity);
                        }
                        Bounds bounds = hit.bounds;
                        _contacts.Add(new RadarContact(
                            bounds.center,
                            Mathf.Max(bounds.extents.x, bounds.extents.z),
                            true));
                    }
                    else if (_layerService.IsInLayer(hit.gameObject, LayerKey.Obstacle))
                    {
                        Bounds bounds = hit.bounds;
                        _contacts.Add(new RadarContact(
                            bounds.center,
                            Mathf.Max(bounds.extents.x, bounds.extents.z),
                            false));
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
                }

                ContactsUpdated?.Invoke(_contacts);
                _timer.StartTimer();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                }
#endif
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
                    _layerService.GetMask(LayerKey.Unit, LayerKey.Obstacle),
                    QueryTriggerInteraction.Collide);
                if (hitAmount < _overlapHits.Length || _overlapHits.Length >= MAX_HIT_LIMIT)
                {
                    return hitAmount;
                }

                int newSize = Math.Min(_overlapHits.Length * 2, MAX_HIT_LIMIT);
                Array.Resize(ref _overlapHits, newSize);
            }
        }

        public override void Release()
        {
            _isReleased = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_radarRangeCircle != null) { _radarRangeCircle.Destroy(); _radarRangeCircle = null; }
#endif
            _contacts.Clear();
            _detectedEnemies.Clear();
            Model.Enemies.Clear();
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
