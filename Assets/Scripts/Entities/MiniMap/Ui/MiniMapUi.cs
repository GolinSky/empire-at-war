using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Controllers.MiniMap;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Views.MiniMap
{
    public interface IMiniMapPositionConvector
    {
        Vector2 GetPosition(Vector3 worldPos);
        Vector2 GetSize(float worldDiameter);
    }
    public class MiniMapUi : BaseUi<IMiniMapModelObserver, IMiniMapCommand>, IPointerDownHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IMiniMapPositionConvector, IInitializable, ILateDisposable
    {
        private const float HIGHLIGHT_DURATION = 0.3f;
        private const float HIGHLIGHT_MAP_ALPHA = 1f;
        private const float HIGHLIGHT_MARK_ALPHA = 1f;
        private const float FADE_DURATION = 0.3f;
        private const float ORIGIN_MAP_ALPHA = 0.8f;

        [SerializeField] private RectTransform miniMapRectTransform;
        [SerializeField] private Transform iconParent;
        [SerializeField] private Image mapImage;
        [SerializeField] private CameraFootprintView cameraFootprintView;

        private List<Image> _mapMarkers = new List<Image>();
        private Dictionary<MiniMapMarker, MarkView> _markerViews =
            new Dictionary<MiniMapMarker, MarkView>();
        private Vector2Range _mapRange;
        private bool _isInteractable = true;
        private Rect MiniMapRect => miniMapRectTransform.rect;

        public void Initialize()
        {
            _mapRange = Model.MapRange;
            AddMark(Model.PlayerBase);
            AddMark(Model.EnemyBase);
            cameraFootprintView.SetData(Model.CameraMark, Model.MapRange);
            foreach (MiniMapMarker marker in Model.Markers)
            {
                AddMarker(marker);
            }
            Model.OnMarkAdded += AddMark;
            Model.OnMarkerAdded += AddMarker;
            Model.OnMarkerRemoved += RemoveMarker;
            Model.OnInteractableChanged += ActivateInteraction;
        }

        public void LateDispose()
        {
            Model.OnMarkAdded -= AddMark;
            Model.OnMarkerAdded -= AddMarker;
            Model.OnMarkerRemoved -= RemoveMarker;
            Model.OnInteractableChanged -= ActivateInteraction;
            cameraFootprintView.DOKill();
        }

        private void ActivateInteraction(bool isActive)
        {
            _isInteractable = isActive;
            mapImage.DOFade(ORIGIN_MAP_ALPHA, FADE_DURATION);
            DoFade(ORIGIN_MAP_ALPHA, FADE_DURATION);
        }

        private void AddMark(MarkData markData)
        {
            MarkView view = Instantiate(Model.MarkViewPrefab);
            view.SetData( iconParent, GetPosition(markData.Position), markData.Icon);
            if (markData == Model.PlayerBase || markData == Model.EnemyBase)
            {
                view.IconImage.color = markData == Model.PlayerBase
                    ? new Color(0.15f, 0.65f, 1f)
                    : new Color(1f, 0.2f, 0.15f);
            }
            _mapMarkers.Add(view.IconImage);
        }

        private void AddMarker(MiniMapMarker marker)
        {
            MarkView view = Instantiate(Model.MarkViewPrefab);
            view.SetData(this, iconParent, marker, Model.GetIcon(marker.MarkType));
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
            Destroy(view.gameObject);
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
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            MoveCamera(eventData);
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
            if (!_isInteractable || Model.IsInputBlocked) return;

            UnityEngine.Camera eventCamera = eventData.pressEventCamera;
            if (!RectTransformUtility.RectangleContainsScreenPoint(miniMapRectTransform, eventData.position, eventCamera))
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                miniMapRectTransform,
                eventData.position,
                eventCamera,
                out Vector2 localPoint);

            float x = Mathf.InverseLerp(MiniMapRect.xMin, MiniMapRect.xMax, localPoint.x);
            float y = Mathf.InverseLerp(MiniMapRect.yMin, MiniMapRect.yMax, localPoint.y);

            Vector3 worldPoint = new Vector3
            {
                x = Mathf.Lerp(_mapRange.Min.x, _mapRange.Max.x, x),
                z = Mathf.Lerp(_mapRange.Min.y, _mapRange.Max.y, y)
            };

            Command.MoveTo(worldPoint);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(!_isInteractable) return;
            if(Model.IsInputBlocked) return;

            DoFade(HIGHLIGHT_MARK_ALPHA, HIGHLIGHT_DURATION);
            mapImage.DOFade(HIGHLIGHT_MAP_ALPHA, HIGHLIGHT_DURATION);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable) return;

            mapImage.DOFade(ORIGIN_MAP_ALPHA, FADE_DURATION);
            DoFade(ORIGIN_MAP_ALPHA, FADE_DURATION);
        }


        private void DoFade(float alpha, float duration)
        {
            cameraFootprintView.DOFade(alpha, duration);
            for (var i = 0; i < _mapMarkers.Count; i++)
            {
                _mapMarkers[i].DOFade(alpha, duration);
            }
        }
    }
}
