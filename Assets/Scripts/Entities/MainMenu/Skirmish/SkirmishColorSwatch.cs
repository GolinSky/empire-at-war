using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public sealed class SkirmishColorSwatch : Button
    {
        public event Action Previewed;
        public event Action PreviewEnded;

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            Previewed?.Invoke();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            PreviewEnded?.Invoke();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            Previewed?.Invoke();
        }
    }
}
