using System.Collections.Generic;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>What one setup row shows besides its slot values: labels, occupant choices and edit rights.</summary>
    public readonly struct SkirmishSlotRowState
    {
        public SkirmishSlotRowState(string title, string role, IReadOnlyList<string> occupantOptions,
            int occupantIndex, bool occupantEditable, bool detailsEditable)
        {
            Title = title;
            Role = role;
            OccupantOptions = occupantOptions;
            OccupantIndex = occupantIndex;
            OccupantEditable = occupantEditable;
            DetailsEditable = detailsEditable;
        }

        public string Title { get; }
        public string Role { get; }
        public IReadOnlyList<string> OccupantOptions { get; }
        public int OccupantIndex { get; }
        public bool OccupantEditable { get; }
        public bool DetailsEditable { get; }
    }
}
