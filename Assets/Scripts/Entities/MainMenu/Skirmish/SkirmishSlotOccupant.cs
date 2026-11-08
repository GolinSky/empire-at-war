namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>Who plays a setup row. Closed .. AiUltraHard match the occupant dropdown of AI rows.</summary>
    public enum SkirmishSlotOccupant
    {
        Closed = 0,
        AiEasy = 1,
        AiMedium = 2,
        AiHard = 3,
        AiUltraHard = 4,
        Human = 5,
        /// <summary>Online lobby: an open seat the next joining player takes; not in the battle.</summary>
        Vacant = 6
    }
}
