using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>What one setup row shows besides its slot values: labels, occupant choices and edit rights.</summary>
    public readonly struct SkirmishSlotRowState
    {
        public SkirmishSlotRowState(string title, string role, IReadOnlyList<string> occupantOptions,
            int occupantIndex, bool occupantEditable, bool detailsEditable, bool teamEditable, Texture avatar)
        {
            Avatar = avatar;
            TeamEditable = teamEditable;
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
        /// <summary>Faction and colour.</summary>
        public bool DetailsEditable { get; }
        public bool TeamEditable { get; }
        /// <summary>Online: the member's Steam avatar in place of the slot number; null shows the number.</summary>
        public Texture Avatar { get; }
    }
}
