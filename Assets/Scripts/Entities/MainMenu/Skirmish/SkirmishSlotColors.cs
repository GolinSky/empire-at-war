using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>Colors stay unique across setup rows: taking a color another row uses swaps the two rows' colors.</summary>
    public static class SkirmishSlotColors
    {
        public static void Select(IReadOnlyList<SkirmishSlotSetup> slots, int slotIndex, int colorIndex)
        {
            if (colorIndex < 0 || colorIndex >= MatchRules.MAX_TEAM_COLORS)
            {
                throw new ArgumentOutOfRangeException(nameof(colorIndex), colorIndex, "No such team color.");
            }

            SkirmishSlotSetup slot = slots[slotIndex];
            foreach (SkirmishSlotSetup other in slots)
            {
                if (other != slot && other.ColorIndex == colorIndex)
                {
                    other.ColorIndex = slot.ColorIndex;
                }
            }

            slot.ColorIndex = colorIndex;
        }
    }
}
