using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using UnityEngine;
using UnityEngine.Rendering;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.Radar;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Utils;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Extensions;

namespace EmpireAtWar.Components.Radar
{
    public interface IRadarComponent : IComponent
    {
        ObservableList<IEntity> Enemies { get; }
        bool HasEnemy(IEntity entity);
    }
    public class RadarComponent : MonoComponent<RadarModel>, IInitializable, ITickable, IOrderedDisposable, IRadarComponent, IRadarScanner,
        IRespawnable
    {
        private IRadarScanService _scanService;
        private ILayerService _layerService;
        private ISelectionModelObserver _selection;

        private readonly HashSet<IEntity> _detectedEnemies = new HashSet<IEntity>();
        // Mirrors Model.Enemies for O(1) membership; ObservableList.Contains is linear.
        private readonly HashSet<IEntity> _currentEnemies = new HashSet<IEntity>();
        private DebugRangeCircleFactory _rangeCircleFactory;
        private Transform _viewTransform;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private DebugRangeCircle _radarRangeCircle;
#endif

        private bool _isReleased;

        public ObservableList<IEntity> Enemies => Model.Enemies;
        public bool HasEnemy(IEntity entity) => _currentEnemies.Contains(entity);

        PlayerId IRadarScanner.Owner => Model.Owner;
        float IRadarScanner.Range => Model.Range;
        float IRadarScanner.Delay => Model.Delay;
        Vector3 IRadarScanner.ScanCenter => _viewTransform.position;

        [Inject]
        private void Construct(IRadarScanService scanService, ILayerService layerService, ISelectionModelObserver selection,
            RadarModel model, DebugRangeCircleFactory rangeCircleFactory,
            [Key(EntityBindType.ViewTransform)] Transform viewTransform)
        {
            SetModel(model);
            _scanService = scanService;
            _layerService = layerService;
            _rangeCircleFactory = rangeCircleFactory;
            _selection = selection;
            _viewTransform = viewTransform;
        }

        public void Initialize()
        {
            _layerService.Apply(gameObject, LayerKey.Unit, true);
            _scanService.Register(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _radarRangeCircle = _rangeCircleFactory.Create("RadarRange", new Color(0.2f, 0.6f, 1f), _selection);
#endif
        }

        public void Respawn()
        {
            _isReleased = false;
            Initialize();
        }

        public void Tick()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_isReleased)
            {
                return;
            }

            _radarRangeCircle.Draw(_viewTransform.position, Model.Range);
#endif
        }

        void IRadarScanner.ApplyScan(IReadOnlyList<IEntity> detectedEnemies)
        {
            if (_isReleased)
            {
                return;
            }

            _detectedEnemies.Clear();
            for (int i = 0; i < detectedEnemies.Count; i++)
            {
                _detectedEnemies.Add(detectedEnemies[i]);
            }

            for (int i = Model.Enemies.Count - 1; i >= 0; i--)
            {
                IEntity enemy = Model.Enemies[i];
                if (!_detectedEnemies.Contains(enemy))
                {
                    _currentEnemies.Remove(enemy);
                    Model.Enemies.RemoveAt(i);
                }
            }

            for (int i = 0; i < detectedEnemies.Count; i++)
            {
                IEntity enemy = detectedEnemies[i];
                if (_currentEnemies.Add(enemy))
                {
                    Model.Enemies.Add(enemy);
                }
            }
        }

        public override void Release()
        {
            // Scene unload disposes the radar before its entity releases its components.
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            _scanService.Unregister(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_radarRangeCircle != null) { _radarRangeCircle.Destroy(); _radarRangeCircle = null; }
#endif
            _detectedEnemies.Clear();
            _currentEnemies.Clear();
            Model.Enemies.Clear();
        }

        // Covers entities destroyed without dying (scene unload, despawn): the scan service must drop them.
        void IOrderedDisposable.Dispose()
        {
            if (!_isReleased)
            {
                Release();
            }
        }
    }
}
