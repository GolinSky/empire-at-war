using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.ControlGroups
{
    public sealed class ControlGroupUi : BaseUi
    {
        [SerializeField] private ControlGroupSlotView[] slots;

        public void RenderSlot(int digit, int count, Sprite first, Sprite second, bool selected)
        {
            slots[digit == 0 ? 9 : digit - 1].Render(count, first, second, selected);
        }

        public void HighlightSlot(int digit)
        {
            slots[digit == 0 ? 9 : digit - 1].Highlight();
        }
    }
}
