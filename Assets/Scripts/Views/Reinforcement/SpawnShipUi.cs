using System;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireAtWar
{
    public class SpawnShipUi : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private Image iconImage;
        
        private ShipType shipType;
        private Action<ShipType, SpawnShipUi> dragAction;
        public void Init(Action<ShipType, SpawnShipUi> dragAction, ShipType shipType, Sprite icon)
        {
            this.dragAction = dragAction;
            iconImage.sprite = icon;
            this.shipType = shipType;
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            dragAction?.Invoke(shipType, this);
        }

        public void Destroy()
        {
            dragAction = null;
            Destroy(gameObject);
        }

        public void OnDrag(PointerEventData eventData)
        {
            
        }
    }
}