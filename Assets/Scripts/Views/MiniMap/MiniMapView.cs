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
    public class MiniMapView : View<IMiniMapModelObserver, IMiniMapCommand>, IPointerMoveHandler, IPointerEnterHandler
    {
        private const float DeltaOffset = 0.5f;
        private const float HighlightDuration = 1f;
        private const float HighlightAlpha = 0.1f;
        private const float FadeDuration = 0.5f;

        [SerializeField] private Button switcherButton;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform miniMapRectTransform;
        [SerializeField] private Transform iconParent;
        [SerializeField] private Image mapImage;
        
        private Vector2Range mapRange;
        private float originAlpha;
        private bool isInteractable = true;
        private Rect MiniMapRect => miniMapRectTransform.rect;

        protected override void OnInitialize()
        {
            originAlpha = mapImage.color.a;
            mapRange = Model.MapRange;
            AddMark(Model.PlayerBase);
            AddMark(Model.EnemyBase);
            Model.OnMarkAdded += AddMark;
            Model.OnInteractableChanged += ActivateInteraction;
            switcherButton.onClick.AddListener(SetCanvasActive);
        }

        protected override void OnDispose()
        {
            Model.OnMarkAdded -= AddMark;
            Model.OnInteractableChanged -= ActivateInteraction;
            switcherButton.onClick.RemoveListener(SetCanvasActive);
        }
        
        private void SetCanvasActive()
        {
            canvas.enabled = !canvas.enabled;
        }

        private void ActivateInteraction(bool isActive)
        {
            isInteractable = isActive;
            mapImage.DOFade(isActive ? originAlpha : 0, FadeDuration);
        }
        
        private void AddMark(MarkData markData)
        {
            MarkView view = Instantiate(Model.MarkViewPrefab);
            view.SetData(iconParent, GetPosition(markData.Position), markData.Icon);
        }

        private Vector2 GetPosition(Vector3 worldPos)
        {
            float x = Mathf.InverseLerp(mapRange.Min.x, mapRange.Max.x, worldPos.x);
            float y = Mathf.InverseLerp(mapRange.Min.y, mapRange.Max.y, worldPos.z);
            
            Vector2 miniMapPos = new Vector2
            {
                x = Mathf.Lerp(MiniMapRect.xMin, MiniMapRect.xMax, x),
                y = Mathf.Lerp(MiniMapRect.yMin, MiniMapRect.yMax, y),
            };
            
            return miniMapPos;
        }
        
        public void OnPointerMove(PointerEventData eventData)
        {
            if(!isInteractable) return;
            if(Model.IsInputBlocked) return;
            
            if (!RectTransformUtility.RectangleContainsScreenPoint(miniMapRectTransform, eventData.position))
            {
                mapImage.DOFade(originAlpha, FadeDuration);
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                miniMapRectTransform,
                eventData.position,
                null,
                out Vector2 localPoint);

            Vector2 percentage;
            percentage.x = localPoint.x / MiniMapRect.width + DeltaOffset;
            percentage.y = localPoint.y / MiniMapRect.height + DeltaOffset;

        
            Vector3 worldPoint = new Vector3
            {
                x = Mathf.Lerp(mapRange.Min.x, mapRange.Max.x, Mathf.Abs(percentage.x)) ,
                z = Mathf.Lerp(mapRange.Min.y, mapRange.Max.y, Mathf.Abs(percentage.y))  
            };
            
            Command.MoveTo(worldPoint);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(!isInteractable) return;

            mapImage.DOFade(HighlightAlpha, HighlightDuration);
        }
    }
}