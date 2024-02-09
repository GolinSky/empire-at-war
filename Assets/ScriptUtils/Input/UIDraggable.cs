using UnityEngine;
using UnityEngine.EventSystems;

namespace EmpireAtWar
{
    public class UIDraggable : MonoBehaviour, IDragHandler 
    {
        public void OnDrag (PointerEventData eventData)
        {
            this.transform.position += (Vector3)eventData.delta;
        }
    }
}