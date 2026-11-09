using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Splits research into economy (income, production) and fleet (combat stats) for separate lessons.</summary>
    public static class ResearchGroup
    {
        public static bool IsEconomy(ResearchType type) =>
            type == ResearchType.IncreasedProduction || type == ResearchType.IncreasedStationIncome;
    }
}
