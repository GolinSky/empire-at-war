namespace EmpireAtWar.Entities.Game
{
    public enum GameMode
    {
        Skirmish = 0,
        Survival = 1,
        BattleSimulation = 2,
        // Guided tutorial battle (Services/Tutorial/Guided). Never reuse this number: saves store it.
        Tutorial = 3,
    }
}
