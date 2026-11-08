using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>One editable row of the skirmish setup screen.</summary>
    public sealed class SkirmishSlotSetup
    {
        public SkirmishSlotOccupant Occupant { get; set; }
        public FactionType Faction { get; set; }
        public int Team { get; set; }
        /// <summary>Index into the team color palette; unique across the rows.</summary>
        public int ColorIndex { get; set; }
        /// <summary>Online lobby: the seated member's connection id, or <see cref="SkirmishModel.NO_MEMBER"/>.</summary>
        public int MemberId { get; set; } = SkirmishModel.NO_MEMBER;
        /// <summary>Online lobby: the seated member's Steam (or PC) name.</summary>
        public string PlayerName { get; set; } = string.Empty;
        /// <summary>Online lobby: the seated member ticked ready (the host always is).</summary>
        public bool Ready { get; set; }
        /// <summary>Online lobby: the seated member's Steam id for the avatar; 0 without Steam.</summary>
        public ulong SteamId { get; set; }
        /// <summary>The row plays in the battle (a human or an AI).</summary>
        public bool IsOpen => Occupant != SkirmishSlotOccupant.Closed && Occupant != SkirmishSlotOccupant.Vacant;
        public bool IsHuman => Occupant == SkirmishSlotOccupant.Human;

        public SkirmishSlotSetup(SkirmishSlotOccupant occupant, FactionType faction, int team, int colorIndex)
        {
            ColorIndex = colorIndex;
            Occupant = occupant;
            Faction = faction;
            Team = team;
        }

        public void ClearMember()
        {
            MemberId = SkirmishModel.NO_MEMBER;
            PlayerName = string.Empty;
            Ready = false;
            SteamId = 0UL;
        }
    }
}
