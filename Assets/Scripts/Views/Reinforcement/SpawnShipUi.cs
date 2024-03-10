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
        ShipType ShipType { get; }
        void DecreaseUnitCount();
        void AddUnit();
        void Activate(bool isActive);
        void Init(IReinforcementVisitor reinforcementVisitor, ShipType shipType, FactionData factionData);
    }
    
    public class SpawnShipUi : MonoBehaviour, IBeginDragHandler, IDragHandler, ISpawnShipUi
    {
        private const int DefaultCountValue = 1;
        
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI unitCapacityText;
        [SerializeField] private TextMeshProUGUI unitCountText;
       
        private IReinforcementVisitor reinforcementVisitor;
        
        private Color originColor;
        private Color blockedColor = Color.gray;
        private int count;
        private bool isBlocked;
        

        public ShipType ShipType { get; private set; }

        private void Awake()
        {
            originColor = backgroundImage.color;
        }

        void ISpawnShipUi.Init(IReinforcementVisitor reinforcementVisitor, ShipType shipType, FactionData factionData)
        {
            this.reinforcementVisitor = reinforcementVisitor;
            iconImage.sprite = factionData.Icon;
            unitCapacityText.text = factionData.UnitCapacity.ToString();
            ShipType = shipType;
            count = DefaultCountValue;
            UpdateUnitCountText();
        }
        
        void ISpawnShipUi.DecreaseUnitCount()
        {
            count--;
            UpdateUnitCountText();
            if (count <= 0)
            {
                Destroy();
            }
        }

        void ISpawnShipUi.AddUnit()
        {
            count++;
            UpdateUnitCountText();
        }
        
        void ISpawnShipUi.Activate(bool isActive)
        {
            backgroundImage.color = isActive ? originColor : blockedColor;
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if(isBlocked) return;
            
            reinforcementVisitor?.Handle(this);
        }
        
        void IDragHandler.OnDrag(PointerEventData eventData) {}
        
        private void UpdateUnitCountText()
        {
            unitCountText.text = count.ToString();
        }
        
        private void Destroy()
        {
            reinforcementVisitor.OnRelease(this);
            reinforcementVisitor = null;
            Destroy(gameObject);
        }
    }
}