using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Controllers.MiniMap;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MiniMap
{
    public interface IMiniMapPositionConvector
    {
        Vector2 GetPosition(Vector3 worldPos);
    }
    public class MiniMapView : View<IMiniMapModelObserver, IMiniMapCommand>, IPointerMoveHandler, IPointerEnterHandler, IMiniMapPositionConvector
    {
        private const float DELTA_OFFSET = 0.5f;
        private const float HIGHLIGHT_DURATION = 1f;
        private const float HIGHLIGHT_MAP_ALPHA = 0.1f;
        private const float HIGHLIGHT_MARK_ALPHA = 1f;
        private const float FADE_DURATION = 0.5f;
        private const float ORIGIN_MAP_ALPHA = 1f;
        
        [SerializeField] private Button switcherButton;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform miniMapRectTransform;
        [SerializeField] private Transform iconParent;
        [SerializeField] private Image mapImage;
        
        private List<Image> _mapMarkers = new List<Image>();
        private Vector2Range _mapRange;
        private bool _isInteractable = true;
        private Rect MiniMapRect => miniMapRectTransform.rect;

        protected override void OnInitialize()
        {
            _mapRange = Model.MapRange;
            AddMark(Model.PlayerBase);
            AddMark(Model.EnemyBase);
            AddDynamicMark(Model.CameraMark);
            Model.OnMarkAdded += AddMark;
            Model.OnDynamicMarkAdded += AddDynamicMark;
            Model.OnInteractableChanged += ActivateInteraction;
            switcherButton.onClick.AddListener(SetCanvasActive);
        }

        protected override void OnDispose()
        {
            Model.OnMarkAdded -= AddMark;
            Model.OnDynamicMarkAdded -= AddDynamicMark;
            Model.OnInteractableChanged -= ActivateInteraction;
            switcherButton.onClick.RemoveListener(SetCanvasActive);
        }

  
        private void SetCanvasActive()
        {
            canvas.enabled = !canvas.enabled;
        }

        private void ActivateInteraction(bool isActive)
        {
            _isInteractable = isActive;
            float targetAlpha = isActive ? ORIGIN_MAP_ALPHA : 0;
            mapImage.DOFade(targetAlpha, FADE_DURATION);
            DoFade(targetAlpha, FADE_DURATION);
        }
        
        private void AddMark(MarkData markData)
        {
            MarkView view = Instantiate(Model.MarkViewPrefab);
            view.SetData( iconParent, GetPosition(markData.Position), markData.Icon);
            _mapMarkers.Add(view.IconImage);
        }
        
        private void AddDynamicMark(DynamicMarkData dynamicMarkData)
        {
            MarkView view = Instantiate(Model.MarkViewPrefab);
            view.SetData(this, iconParent, dynamicMarkData);
            _mapMarkers.Add(view.IconImage);
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
        
        public void OnPointerMove(PointerEventData eventData)
        {
            if(!_isInteractable) return;
            if(Model.IsInputBlocked) return;
            
            if (!RectTransformUtility.RectangleContainsScreenPoint(miniMapRectTransform, eventData.position))
            {
                mapImage.DOFade(ORIGIN_MAP_ALPHA, FADE_DURATION);
                DoFade(ORIGIN_MAP_ALPHA, FADE_DURATION);
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                miniMapRectTransform,
                eventData.position,
                null,
                out Vector2 localPoint);

            Vector2 percentage;
            percentage.x = localPoint.x / MiniMapRect.width + DELTA_OFFSET;
            percentage.y = localPoint.y / MiniMapRect.height + DELTA_OFFSET;

            Vector3 worldPoint = new Vector3
            {
                x = Mathf.Lerp(_mapRange.Min.x, _mapRange.Max.x, Mathf.Abs(percentage.x)),
                z = Mathf.Lerp(_mapRange.Min.y, _mapRange.Max.y, Mathf.Abs(percentage.y))  
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


        private void DoFade(float alpha, float duration)
        {
            for (var i = 0; i < _mapMarkers.Count; i++)
            {
                _mapMarkers[i].DOFade(alpha, duration);
            }
        }
    }
}