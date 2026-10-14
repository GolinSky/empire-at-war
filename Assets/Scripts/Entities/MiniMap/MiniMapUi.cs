using EmpireAtWar.Components.Ui.Tooltip;
using System;
using EmpireAtWar.Services.Player;
using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EmpireAtWar.Views.MiniMap;

namespace EmpireAtWar.Entities.MiniMap
{
    public class MiniMapUi : BaseUi, IPointerDownHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IMiniMapPositionConvector
    {
        private const float HIGHLIGHT_DURATION = 0.3f;
        private const float HIGHLIGHT_MAP_ALPHA = 1f;
        private const float HIGHLIGHT_MARK_ALPHA = 1f;
        private const float FADE_DURATION = 0.3f;
        private const float ORIGIN_MAP_ALPHA = 0.8f;

        [SerializeField] private RectTransform miniMapRectTransform;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private Transform iconParent;
        [SerializeField] private Image mapImage;
        [SerializeField] private CameraFootprintView cameraFootprintView;
        [SerializeField] private MiniMapObstacleView obstacleView;
        [SerializeField] private MiniMapMoveTargetView moveTargetView;
        [SerializeField] private RawImage fogImage;
        private List<Image> _mapMarkers = new List<Image>();
        private Dictionary<MiniMapMarker, MarkView> _markerViews =
            new Dictionary<MiniMapMarker, MarkView>();
        private Vector2Range _mapRange;
        private Tween _markerFade;

        private float _markerAlpha = HIGHLIGHT_MARK_ALPHA;

        private IMiniMapModelObserver _model;
        private IPlayerColors _playerColors;
        private IMiniMapPresenter _presenter;
        private bool _isInitialized;

        public void SetData(IMiniMapModelObserver model, IPlayerColors playerColors)
        {
            _model = model;
            _playerColors = playerColors;
        }

        public void AssignPresenter(IMiniMapPresenter presenter)
        {
            if (_presenter != null) throw new InvalidOperationException("Minimap presenter is already assigned.");
            _presenter = presenter;
        }
        public TooltipHoverView TooltipHover => tooltipHover;
        private Rect MiniMapRect => miniMapRectTransform.rect;

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            _mapRange = _model.MapRange;
            cameraFootprintView.SetData(_model.CameraMark, _model.MapRange);
            obstacleView.SetData(_model.Obstacles, _model.MapRange);
            foreach (MiniMapMarker marker in _model.Markers)
            {
                AddMarker(marker);
            }
            _model.OnMarkAdded += AddMark;
            _model.OnMarkerAdded += AddMarker;
            _model.OnMarkerRemoved += RemoveMarker;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            _isInitialized = false;
            _model.OnMarkAdded -= AddMark;
            _model.OnMarkerAdded -= AddMarker;
            _model.OnMarkerRemoved -= RemoveMarker;
            if (_markerFade != null) _markerFade.Kill();
            foreach (MarkView view in _markerViews.Values) view.Release();
            // Inactive prefabs may be destroyed without Unity invoking OnDestroy.
            if (this == null) return;
            mapImage.DOKill();
            cameraFootprintView.DOKill();
            obstacleView.DOKill();
        }

        private void OnDestroy() => Dispose();

        // Markers only redraw when they change, so a resized minimap places them again.
        private void OnRectTransformDimensionsChange()
        {
            foreach (MarkView view in _markerViews.Values)
            {
                view.Refresh();
            }
        }

        public void SetFog(Texture mask)
        {
            fogImage.texture = mask;
        }

        private void AddMark(MarkData markData)
        {
            MarkView view = Instantiate(_model.MarkViewPrefab);
            view.SetData( iconParent, GetPosition(markData.Position), markData.Icon);
            tooltipHover.Register(view.TooltipTrigger);
            _mapMarkers.Add(view.IconImage);
        }

        private void AddMarker(MiniMapMarker marker)
        {
            MarkView view = Instantiate(_model.MarkViewPrefab);
            view.SetData(this, _playerColors, iconParent, marker, _model.GetIcon(marker.MarkType));
            tooltipHover.Register(view.TooltipTrigger);
            _markerViews.Add(marker, view);
            _mapMarkers.Add(view.IconImage);
        }

        private void RemoveMarker(MiniMapMarker marker)
        {
            if (!_markerViews.Remove(marker, out MarkView view))
            {
                return;
            }

            _mapMarkers.Remove(view.IconImage);
            view.Release();
            // Scene teardown can destroy the view before the marker presenter is disposed.
            if (view != null)
            {
                if (Application.isPlaying) Destroy(view.gameObject);
                else DestroyImmediate(view.gameObject);
            }
        }

        public Vector2 GetPosition(Vector3 worldPos)
        {
            float x = Mathf.InverseLerp(_mapRange.Min.x, _mapRange.Max.x, worldPos.x);
            float y = Mathf.InverseLerp(_mapRange.Min.y, _mapRange.Max.y, worldPos.z);

            Vector2 miniMapPos = new Vector2
            {
                x = Mathf.Lerp(MiniMapRect.xMin, MiniMapRect.xMax, x),
                y = Mathf.Lerp(MiniMapRect.yMin, MiniMapRect.yMax, y),
            };

            return miniMapPos;
        }

        public Vector2 GetSize(float worldDiameter)
        {
            return new Vector2(
                MiniMapRect.width * worldDiameter / (_mapRange.Max.x - _mapRange.Min.x),
                MiniMapRect.height * worldDiameter / (_mapRange.Max.y - _mapRange.Min.y));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    MoveCamera(eventData);
                    break;
                case PointerEventData.InputButton.Right:
                    OrderMove(eventData);
                    break;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            MoveCamera(eventData);
        }

        private void MoveCamera(PointerEventData eventData)
        {
            if (_model.IsInputBlocked) return;
            if (!TryGetWorldPoint(eventData, out Vector3 worldPoint)) return;

            _presenter.MoveCamera(worldPoint);
        }

        private void OrderMove(PointerEventData eventData)
        {
            if (_model.IsInputBlocked) return;
            if (!TryGetWorldPoint(eventData, out Vector3 worldPoint)) return;

            _presenter.OrderMove(worldPoint);
        }

        public void PlayMoveTarget(Vector3 worldPoint)
        {
            moveTargetView.Play(GetPosition(worldPoint));
        }

        private bool TryGetWorldPoint(PointerEventData eventData, out Vector3 worldPoint)
        {
            worldPoint = default;
            UnityEngine.Camera eventCamera = eventData.pressEventCamera;
            if (!RectTransformUtility.RectangleContainsScreenPoint(miniMapRectTransform, eventData.position, eventCamera))
            {
                return false;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                miniMapRectTransform,
                eventData.position,
                eventCamera,
                out Vector2 localPoint);

            float x = Mathf.InverseLerp(MiniMapRect.xMin, MiniMapRect.xMax, localPoint.x);
            float y = Mathf.InverseLerp(MiniMapRect.yMin, MiniMapRect.yMax, localPoint.y);

            worldPoint = new Vector3
            {
                x = Mathf.Lerp(_mapRange.Min.x, _mapRange.Max.x, x),
                z = Mathf.Lerp(_mapRange.Min.y, _mapRange.Max.y, y)
            };
            return true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(_model.IsInputBlocked) return;

            DoFade(HIGHLIGHT_MARK_ALPHA, HIGHLIGHT_DURATION);
            mapImage.DOKill();
            mapImage.DOFade(HIGHLIGHT_MAP_ALPHA, HIGHLIGHT_DURATION).SetLink(gameObject);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            mapImage.DOKill();
            mapImage.DOFade(ORIGIN_MAP_ALPHA, FADE_DURATION).SetLink(gameObject);
            DoFade(ORIGIN_MAP_ALPHA, FADE_DURATION);
        }

        private void DoFade(float alpha, float duration)
        {
            cameraFootprintView.DOKill();
            obstacleView.DOKill();
            cameraFootprintView.DOFade(alpha, duration).SetLink(gameObject);
            obstacleView.DOFade(alpha, duration).SetLink(gameObject);
            if (_markerFade != null) _markerFade.Kill();
            _markerFade = DOTween.To(() => _markerAlpha, UpdateMarkerAlpha, alpha, duration)
                .SetLink(gameObject).OnKill(() => _markerFade = null);
        }

        private void UpdateMarkerAlpha(float alpha)
        {
            _markerAlpha = alpha;
            for (var i = 0; i < _mapMarkers.Count; i++)
            {
                Color color = _mapMarkers[i].color;
                color.a = alpha;
                _mapMarkers[i].color = color;
            }
        }
    }
}
