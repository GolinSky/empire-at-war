using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.Reinforcement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireAtWar
{
    public interface ISpawnShipUi
    {
        string UnitType { get; }
        void DecreaseUnitCount();
        void AddUnit();
        void Activate(bool isActive);
        void Init(IReinforcementVisitor reinforcementVisitor, string unitType, FactionData factionData);
    }
    
    public class SpawnShipUi : MonoBehaviour, IBeginDragHandler, IDragHandler, ISpawnShipUi
    {
        private const int DEFAULT_COUNT_VALUE = 1;
        
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI unitCapacityText;
        [SerializeField] private TextMeshProUGUI unitCountText;
       
        private IReinforcementVisitor _reinforcementVisitor;
        
        private Color _originColor;
        private Color _blockedColor = Color.gray;
        private int _count;
        private bool _isBlocked;
        
        public string UnitType { get; private set; }

        private void Awake()
        {
            _originColor = backgroundImage.color;
        }

        void ISpawnShipUi.Init(IReinforcementVisitor reinforcementVisitor, string unitType, FactionData factionData)
        {
            UnitType = unitType;
            _reinforcementVisitor = reinforcementVisitor;
            iconImage.sprite = factionData.Icon;
            unitCapacityText.text = factionData.UnitCapacity.ToString();
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

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if(_isBlocked) return;
            
            _reinforcementVisitor?.Handle(this);
        }
        
        void IDragHandler.OnDrag(PointerEventData eventData) {}
        
        private void UpdateUnitCountText()
        {
            unitCountText.text = _count.ToString();
        }
        
        private void Destroy()
        {
            _reinforcementVisitor.OnRelease(this);
            _reinforcementVisitor = null;
            Destroy(gameObject);
        }
    }
}