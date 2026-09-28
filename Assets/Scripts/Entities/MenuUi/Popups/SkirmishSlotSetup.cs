using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.MenuUi.Popups
{
    /// <summary>One editable row of the skirmish setup screen.</summary>
    public sealed class SkirmishSlotSetup
    {
        public SkirmishSlotSetup(SkirmishSlotOccupant occupant, FactionType faction, int team)
        {
            Occupant = occupant;
            Faction = faction;
            Team = team;
        }

        public SkirmishSlotOccupant Occupant { get; set; }
        public FactionType Faction { get; set; }
        public int Team { get; set; }
        public bool IsOpen => Occupant != SkirmishSlotOccupant.Closed;
        public bool IsHuman => Occupant == SkirmishSlotOccupant.Human;
    }
}
