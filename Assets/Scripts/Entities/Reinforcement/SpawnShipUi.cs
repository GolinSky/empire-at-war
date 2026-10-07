using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Views.Reinforcement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireAtWar
{
    public interface ISpawnShipUi
    {
        UnitRequest Request { get; }

        void DecreaseUnitCount();

        void AddUnit();

        void Activate(bool isActive);

        void Init(IReinforcementVisitor reinforcementVisitor, UnitRequest request, ScrollRect scrollRect);
    }
    
    public class SpawnShipUi : MonoBehaviour, IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, ISpawnShipUi
    {
        private const int DEFAULT_COUNT_VALUE = 1;

        private IReinforcementVisitor _reinforcementVisitor;

        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI unitCapacityText;
        [SerializeField] private TextMeshProUGUI unitCountText;
        [SerializeField] private TooltipTrigger tooltipTrigger;
        private ScrollRect _scrollRect;

        private Color _originColor;
        private Color _blockedColor = Color.gray;

        private int _count;

        private bool _isScrolling;
        private bool _isBlocked;

        public TooltipTrigger TooltipTrigger => tooltipTrigger;

        public UnitRequest Request { get; private set; }

        private void Awake()
        {
            _originColor = backgroundImage.color;
        }

        void ISpawnShipUi.Init(IReinforcementVisitor reinforcementVisitor, UnitRequest request,
            ScrollRect scrollRect)
        {
            Request = request;
            tooltipTrigger.SetKey(request);
            _reinforcementVisitor = reinforcementVisitor;
            _scrollRect = scrollRect;
            iconImage.sprite = request.FactionData.Icon;
            unitCapacityText.text = $"{request.FactionData.UnitCapacity} CAP";
            _count = DEFAULT_COUNT_VALUE;
            UpdateUnitCountText();
        }

        void ISpawnShipUi.DecreaseUnitCount()
        {
            _count--;
            UpdateUnitCountText();
            if (_count <= 0)
            {
                Destroy();
            }
        }

        void ISpawnShipUi.AddUnit()
        {
            _count++;
            UpdateUnitCountText();
        }

        void ISpawnShipUi.Activate(bool isActive)
        {
            backgroundImage.color = isActive ? _originColor : _blockedColor;
            _isBlocked = !isActive;
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData) =>
            _scrollRect.OnInitializePotentialDrag(eventData);

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - eventData.pressPosition;
            _isScrolling = Mathf.Abs(delta.y) >= Mathf.Abs(delta.x);
            if (_isScrolling)
                _scrollRect.OnBeginDrag(eventData);
            else
                StartPlacement();
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!_isScrolling) return;

            // A vertical drag that leaves the list is a placement, not a scroll.
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    _scrollRect.viewport, eventData.position, eventData.pressEventCamera))
            {
                _scrollRect.OnDrag(eventData);
                return;
            }

            _scrollRect.OnEndDrag(eventData);
            _isScrolling = false;
            StartPlacement();
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (_isScrolling) _scrollRect.OnEndDrag(eventData);
            _isScrolling = false;
        }

        private void UpdateUnitCountText()
        {
            unitCountText.text = $"x{_count}";
        }

        private void StartPlacement()
        {
            if (!_isBlocked)
                _reinforcementVisitor.Handle(this);
        }

        private void Destroy()
        {
            _reinforcementVisitor.OnRelease(this);
            _reinforcementVisitor = null;
            Destroy(gameObject);
        }
    }
}
