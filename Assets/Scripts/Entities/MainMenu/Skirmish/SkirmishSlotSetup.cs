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
        public bool IsOpen => Occupant != SkirmishSlotOccupant.Closed;
        public bool IsHuman => Occupant == SkirmishSlotOccupant.Human;

        public SkirmishSlotSetup(SkirmishSlotOccupant occupant, FactionType faction, int team, int colorIndex)
        {
            ColorIndex = colorIndex;
            Occupant = occupant;
            Faction = faction;
            Team = team;
        }
    }
}
